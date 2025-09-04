using Microsoft.Extensions.DependencyInjection;
using Mpm.Data;
using Mpm.Data.Services;
using Microsoft.EntityFrameworkCore;
using Mpm.Services.Configuration;
using Mpm.Services.Interfaces;
using Mpm.Services.Services;
using Microsoft.Extensions.Configuration;

namespace Mpm.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMpmServices(this IServiceCollection services)
    {
        // Add tenant service
        services.AddScoped<ITenantService, TenantService>();
        
        // Add business services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ISheetService, SheetService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ISteelGradeService, SteelGradeService>();
        services.AddScoped<IProfileTypeService, ProfileTypeService>();
        
        return services;
    }

    /// <summary>
    /// Adds Azure Key Vault configuration and services.
    /// </summary>
    public static IServiceCollection AddKeyVault(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Azure Key Vault options
        services.Configure<AzureKeyVaultOptions>(configuration.GetSection(AzureKeyVaultOptions.SectionName));
        
        // Register the Key Vault service only if enabled
        var keyVaultOptions = configuration.GetSection(AzureKeyVaultOptions.SectionName).Get<AzureKeyVaultOptions>();
        if (keyVaultOptions?.Enabled == true && !string.IsNullOrEmpty(keyVaultOptions.VaultUri))
        {
            services.AddScoped<IKeyVaultService, KeyVaultService>();
        }
        
        return services;
    }
    
    public static IServiceCollection AddMpmDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MpmDbContext>((serviceProvider, options) =>
        {
            var tenantService = serviceProvider.GetRequiredService<ITenantService>();
            var tenantConnectionString = tenantService.GetConnectionString();
            
            options.UseSqlServer(tenantConnectionString.IsNullOrEmpty() ? connectionString : tenantConnectionString);
        });
        
        return services;
    }
}

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }
}
