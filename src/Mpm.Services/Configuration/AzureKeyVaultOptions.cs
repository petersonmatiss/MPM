using System.ComponentModel.DataAnnotations;

namespace Mpm.Services.Configuration;

/// <summary>
/// Configuration options for Azure Key Vault integration.
/// </summary>
public class AzureKeyVaultOptions
{
    public const string SectionName = "AzureKeyVault";

    /// <summary>
    /// The URI of the Azure Key Vault instance.
    /// </summary>
    [Required]
    [Url]
    public string VaultUri { get; set; } = string.Empty;

    /// <summary>
    /// The client ID for authenticating with Azure Key Vault (optional for managed identity).
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The client secret for authenticating with Azure Key Vault (optional for managed identity).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The tenant ID for authenticating with Azure Key Vault (optional for managed identity).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Whether to use managed identity for authentication (default: true).
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Whether Key Vault integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Reload interval for secrets in minutes (default: 60 minutes).
    /// </summary>
    public int ReloadIntervalMinutes { get; set; } = 60;
}