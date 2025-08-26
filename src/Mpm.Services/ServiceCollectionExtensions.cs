using Microsoft.Extensions.DependencyInjection;
using Mpm.Data;
using Mpm.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMpmServices(this IServiceCollection services)
    {
        // Add tenant service
        services.AddScoped<ITenantService, TenantService>();
        
        // Add business services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IQuotationService, QuotationService>();
        
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
