using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mpm.Services.Configuration;

namespace Mpm.Services.Extensions;

/// <summary>
/// Extension methods for configuring Azure Key Vault as a configuration source.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="logger">Optional logger for configuration messages.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddMpmKeyVault(this IConfigurationBuilder builder, ILogger? logger = null)
    {
        // Build intermediate configuration to read Key Vault settings
        var tempConfig = builder.Build();
        var keyVaultOptions = tempConfig.GetSection(AzureKeyVaultOptions.SectionName).Get<AzureKeyVaultOptions>();

        if (keyVaultOptions?.Enabled != true)
        {
            // Use Console.WriteLine for early configuration logging since ILogger might not be available
            Console.WriteLine("Azure Key Vault configuration is disabled or not found");
            return builder;
        }

        if (string.IsNullOrEmpty(keyVaultOptions.VaultUri))
        {
            Console.WriteLine("Azure Key Vault URI is not configured");
            return builder;
        }

        try
        {
            Azure.Core.TokenCredential credential;
            if (keyVaultOptions.UseManagedIdentity)
            {
                credential = new DefaultAzureCredential();
            }
            else
            {
                credential = new ClientSecretCredential(
                    keyVaultOptions.TenantId, 
                    keyVaultOptions.ClientId, 
                    keyVaultOptions.ClientSecret);
            }

            var secretManager = new ConfigurationKeyVaultSecretManager();
            
            builder.AddAzureKeyVault(new Uri(keyVaultOptions.VaultUri), credential, secretManager);

            Console.WriteLine($"Azure Key Vault configuration source added successfully for vault: {keyVaultOptions.VaultUri}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add Azure Key Vault configuration source: {ex.Message}");
            // Don't throw - continue without Key Vault if it fails during configuration
        }

        return builder;
    }
}

/// <summary>
/// Custom secret manager that transforms Key Vault secret names to configuration keys.
/// </summary>
public class ConfigurationKeyVaultSecretManager : Azure.Extensions.AspNetCore.Configuration.Secrets.KeyVaultSecretManager
{
    public override string GetKey(Azure.Security.KeyVault.Secrets.KeyVaultSecret secret)
    {
        // Transform secret names: replace -- with : for hierarchical configuration
        // e.g., "ConnectionStrings--DefaultConnection" becomes "ConnectionStrings:DefaultConnection"
        return secret.Name.Replace("--", ":");
    }
}