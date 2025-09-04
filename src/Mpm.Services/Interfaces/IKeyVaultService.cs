namespace Mpm.Services.Interfaces;

/// <summary>
/// Service interface for Azure Key Vault operations.
/// </summary>
public interface IKeyVaultService
{
    /// <summary>
    /// Retrieves a secret from Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secret value, or null if not found.</returns>
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a secret in Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to set.</param>
    /// <param name="secretValue">The value of the secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret from Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all secret names in the Key Vault.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of secret names.</returns>
    Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Key Vault is available and accessible.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Key Vault is accessible, false otherwise.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}