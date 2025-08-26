using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Mpm.Data.Services;

public interface ITenantService
{
    string GetTenantId();
    string GetConnectionString();
}

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public TenantService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public string GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return "default";

        // Extract tenant from subdomain (e.g., tenant1.mpm.com)
        var host = httpContext.Request.Host.Value;
        var parts = host.Split('.');
        
        if (parts.Length > 1 && !parts[0].Equals("www", StringComparison.OrdinalIgnoreCase))
        {
            return parts[0].ToLowerInvariant();
        }

        // Fallback to header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
        {
            return tenantId.FirstOrDefault() ?? "default";
        }

        return "default";
    }

    public string GetConnectionString()
    {
        var tenantId = GetTenantId();
        var connectionString = _configuration.GetConnectionString($"Tenant_{tenantId}");
        
        // Fallback to default connection string
        return connectionString ?? _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }
}