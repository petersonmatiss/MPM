using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mpm.Services.Configuration;
using Mpm.Services.Interfaces;

namespace Mpm.Services.Services;

/// <summary>
/// Service implementation for Azure Key Vault operations.
/// </summary>
public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultService> _logger;
    private readonly AzureKeyVaultOptions _options;

    public KeyVaultService(IOptions<AzureKeyVaultOptions> options, ILogger<KeyVaultService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!_options.Enabled)
        {
            _logger.LogWarning("Azure Key Vault is disabled in configuration");
            throw new InvalidOperationException("Azure Key Vault is not enabled");
        }

        if (string.IsNullOrEmpty(_options.VaultUri))
        {
            _logger.LogError("Azure Key Vault URI is not configured");
            throw new ArgumentException("VaultUri must be provided in AzureKeyVault configuration");
        }

        var credential = CreateCredential();
        _secretClient = new SecretClient(new Uri(_options.VaultUri), credential);

        _logger.LogInformation("KeyVaultService initialized for vault: {VaultUri}", _options.VaultUri);
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        try
        {
            _logger.LogDebug("Retrieving secret: {SecretName}", secretName);
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            _logger.LogDebug("Successfully retrieved secret: {SecretName}", secretName);
            return response.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret not found: {SecretName}", secretName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret: {SecretName}", secretName);
            throw;
        }
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (secretValue == null)
        {
            throw new ArgumentNullException(nameof(secretValue));
        }

        try
        {
            _logger.LogDebug("Setting secret: {SecretName}", secretName);
            await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
            _logger.LogInformation("Successfully set secret: {SecretName}", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret: {SecretName}", secretName);
            throw;
        }
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        try
        {
            _logger.LogDebug("Deleting secret: {SecretName}", secretName);
            var operation = await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);
            _logger.LogInformation("Successfully deleted secret: {SecretName}", secretName);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret not found for deletion: {SecretName}", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret: {SecretName}", secretName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing all secret names");
            var secretNames = new List<string>();
            
            await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                secretNames.Add(secretProperties.Name);
            }

            _logger.LogDebug("Successfully listed {Count} secrets", secretNames.Count);
            return secretNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secret names");
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Key Vault availability");
            // Try to list secrets as a health check - just get the first one
            await foreach (var _ in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                // If we can enumerate at least one secret property, the service is available
                break;
            }
            
            _logger.LogDebug("Key Vault is available");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Key Vault is not available");
            return false;
        }
    }

    private TokenCredential CreateCredential()
    {
        if (_options.UseManagedIdentity)
        {
            _logger.LogInformation("Using managed identity for Key Vault authentication");
            return new DefaultAzureCredential();
        }

        if (string.IsNullOrEmpty(_options.ClientId) || 
            string.IsNullOrEmpty(_options.ClientSecret) || 
            string.IsNullOrEmpty(_options.TenantId))
        {
            _logger.LogError("Client credentials are incomplete for Key Vault authentication");
            throw new InvalidOperationException("When not using managed identity, ClientId, ClientSecret, and TenantId must all be provided");
        }

        _logger.LogInformation("Using client secret credentials for Key Vault authentication");
        return new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
    }
}