# Azure Key Vault Integration

This document describes how to configure and use Azure Key Vault integration in the MPM project for secure secret management.

## Overview

The MPM project integrates with Azure Key Vault to securely manage secrets and configuration values. This integration supports:

- ✅ Managed Identity authentication (recommended for Azure-hosted applications)
- ✅ Service Principal authentication (for local development)
- ✅ Automatic configuration loading from Key Vault
- ✅ Runtime secret retrieval and management
- ✅ Hierarchical configuration mapping

## Configuration

### Basic Setup

Add the following configuration to your `appsettings.json`:

```json
{
  "AzureKeyVault": {
    "Enabled": true,
    "VaultUri": "https://mpm-test-server.vault.azure.net/",
    "UseManagedIdentity": true,
    "ReloadIntervalMinutes": 60
  }
}
```

### Configuration Options

| Property | Description | Default | Required |
|----------|-------------|---------|----------|
| `Enabled` | Enable/disable Key Vault integration | `true` | Yes |
| `VaultUri` | URI of the Azure Key Vault instance | | Yes |
| `UseManagedIdentity` | Use managed identity for authentication | `true` | No |
| `ClientId` | Service principal client ID (if not using managed identity) | | No |
| `ClientSecret` | Service principal client secret (if not using managed identity) | | No |
| `TenantId` | Azure tenant ID (if not using managed identity) | | No |
| `ReloadIntervalMinutes` | How often to reload secrets (minutes) | `60` | No |

### Environment-Specific Configuration

For **local development**, Key Vault is disabled by default in `appsettings.Development.json`:

```json
{
  "AzureKeyVault": {
    "Enabled": false
  }
}
```

For **production/staging**, enable Key Vault and use managed identity:

```json
{
  "AzureKeyVault": {
    "Enabled": true,
    "VaultUri": "https://your-keyvault.vault.azure.net/",
    "UseManagedIdentity": true
  }
}
```

## Authentication Methods

### 1. Managed Identity (Recommended)

For applications running in Azure (App Service, Azure Functions, etc.):

```json
{
  "AzureKeyVault": {
    "UseManagedIdentity": true
  }
}
```

**Prerequisites:**
- Enable System-assigned managed identity on your Azure resource
- Grant the managed identity `Key Vault Secrets User` role on the Key Vault

### 2. Service Principal

For local development or non-Azure environments:

```json
{
  "AzureKeyVault": {
    "UseManagedIdentity": false,
    "ClientId": "your-app-registration-client-id",
    "ClientSecret": "your-client-secret",
    "TenantId": "your-azure-tenant-id"
  }
}
```

**Prerequisites:**
- Create an App Registration in Azure Active Directory
- Generate a client secret
- Grant the service principal `Key Vault Secrets User` role on the Key Vault

## Usage

### 1. Configuration Loading

Secrets are automatically loaded as configuration values during application startup. Use hierarchical naming in Key Vault:

**Key Vault Secret Name:** `ConnectionStrings--DefaultConnection`  
**Configuration Key:** `ConnectionStrings:DefaultConnection`

### 2. Direct Secret Access

Inject and use the `IKeyVaultService` for runtime secret operations:

```csharp
public class MyService
{
    private readonly IKeyVaultService _keyVaultService;

    public MyService(IKeyVaultService keyVaultService)
    {
        _keyVaultService = keyVaultService;
    }

    public async Task<string> GetSecretAsync()
    {
        return await _keyVaultService.GetSecretAsync("my-secret-name");
    }

    public async Task SetSecretAsync(string name, string value)
    {
        await _keyVaultService.SetSecretAsync(name, value);
    }
}
```

### 3. Health Checks

Check Key Vault availability:

```csharp
var isAvailable = await _keyVaultService.IsAvailableAsync();
if (isAvailable)
{
    // Key Vault is accessible
}
```

## Secret Naming Conventions

Use these naming patterns for automatic configuration mapping:

| Configuration Section | Key Vault Secret Name | Example |
|----------------------|----------------------|---------|
| Connection Strings | `ConnectionStrings--{Name}` | `ConnectionStrings--DefaultConnection` |
| App Settings | `AppSettings--{Name}` | `AppSettings--ApiKey` |
| Nested Configuration | `Section--SubSection--Property` | `Logging--LogLevel--Default` |

## Security Best Practices

1. **Use Managed Identity** when possible for Azure-hosted applications
2. **Rotate secrets regularly** using Azure Key Vault's built-in rotation features
3. **Limit access** using Azure RBAC with minimal required permissions
4. **Monitor access** using Azure Key Vault logging and alerts
5. **Never commit secrets** to source control - use Key Vault or local development tools

## Local Development

For local development, you have several options:

### Option 1: Disable Key Vault
Set `AzureKeyVault:Enabled: false` in `appsettings.Development.json` and use local configuration.

### Option 2: Use Azure CLI Authentication
1. Install Azure CLI and run `az login`
2. Ensure you have access to the Key Vault
3. Set `UseManagedIdentity: true` - DefaultAzureCredential will use CLI credentials

### Option 3: Use Service Principal
Create a development service principal and configure client credentials.

## Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Verify managed identity is enabled and has correct permissions
   - Check service principal credentials and permissions
   - Ensure Azure CLI is logged in for local development

2. **Key Vault Not Found**
   - Verify the `VaultUri` is correct
   - Ensure the Key Vault exists and is accessible

3. **Secrets Not Loading**
   - Check secret names match the expected pattern (`--` for hierarchy)
   - Verify permissions include `Key Vault Secrets User` role
   - Check application logs for detailed error messages

### Debugging

Enable detailed logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Mpm.Services.Services.KeyVaultService": "Debug",
      "Azure": "Information"
    }
  }
}
```

## Examples

### Example 1: Connection String from Key Vault

**Key Vault Secret:**
- Name: `ConnectionStrings--DefaultConnection`
- Value: `Server=myserver;Database=mydb;...`

**Usage in Code:**
```csharp
var connectionString = configuration.GetConnectionString("DefaultConnection");
// Value is automatically loaded from Key Vault
```

### Example 2: API Key Management

**Code:**
```csharp
public class ApiService
{
    private readonly IKeyVaultService _keyVault;

    public ApiService(IKeyVaultService keyVault)
    {
        _keyVault = keyVault;
    }

    public async Task<string> GetApiKeyAsync()
    {
        return await _keyVault.GetSecretAsync("third-party-api-key");
    }
}
```

## Support

For issues with Azure Key Vault integration:

1. Check the application logs for error details
2. Verify Azure permissions and configuration
3. Test Key Vault access using Azure CLI: `az keyvault secret show --vault-name your-vault --name secret-name`
4. Review Azure Key Vault access policies and RBAC assignments