using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mpm.Services;
using Mpm.Services.Configuration;
using Mpm.Services.Interfaces;
using Mpm.Services.Services;
using Xunit;

namespace MPM.Tests;

public class KeyVaultServiceTests
{
    [Fact]
    public void AzureKeyVaultOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new AzureKeyVaultOptions();

        // Assert
        Assert.True(options.UseManagedIdentity);
        Assert.True(options.Enabled);
        Assert.Equal(60, options.ReloadIntervalMinutes);
        Assert.Equal("AzureKeyVault", AzureKeyVaultOptions.SectionName);
    }

    [Fact]
    public void KeyVaultService_ShouldThrowWhenDisabled()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(@"
            {
                ""AzureKeyVault"": {
                    ""Enabled"": false,
                    ""VaultUri"": ""https://test.vault.azure.net/""
                }
            }")))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<AzureKeyVaultOptions>(configuration.GetSection(AzureKeyVaultOptions.SectionName));
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<AzureKeyVaultOptions>>();
        var logger = serviceProvider.GetRequiredService<ILogger<KeyVaultService>>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new KeyVaultService(options, logger));
    }

    [Fact]
    public void KeyVaultService_ShouldThrowWhenVaultUriEmpty()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(@"
            {
                ""AzureKeyVault"": {
                    ""Enabled"": true,
                    ""VaultUri"": """"
                }
            }")))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<AzureKeyVaultOptions>(configuration.GetSection(AzureKeyVaultOptions.SectionName));
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<AzureKeyVaultOptions>>();
        var logger = serviceProvider.GetRequiredService<ILogger<KeyVaultService>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new KeyVaultService(options, logger));
    }

    [Fact]
    public void ServiceCollectionExtensions_AddKeyVault_ShouldRegisterServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(@"
            {
                ""AzureKeyVault"": {
                    ""Enabled"": true,
                    ""VaultUri"": ""https://test.vault.azure.net/""
                }
            }")))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKeyVault(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var keyVaultService = serviceProvider.GetService<IKeyVaultService>();
        Assert.NotNull(keyVaultService);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddKeyVault_ShouldNotRegisterWhenDisabled()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(@"
            {
                ""AzureKeyVault"": {
                    ""Enabled"": false,
                    ""VaultUri"": ""https://test.vault.azure.net/""
                }
            }")))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKeyVault(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var keyVaultService = serviceProvider.GetService<IKeyVaultService>();
        Assert.Null(keyVaultService);
    }
}