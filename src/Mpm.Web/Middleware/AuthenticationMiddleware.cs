using Mpm.Services;

namespace Mpm.Web.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthenticationService authService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip authentication for public paths
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // Try to get session token from various sources
        var sessionToken = GetSessionToken(context);

        if (string.IsNullOrEmpty(sessionToken))
        {
            await RedirectToLogin(context);
            return;
        }

        // Validate session
        var isValidSession = await authService.ValidateSessionAsync(sessionToken);
        if (!isValidSession)
        {
            await RedirectToLogin(context);
            return;
        }

        // Get user and add to context
        var user = await authService.GetUserBySessionTokenAsync(sessionToken);
        if (user != null)
        {
            context.Items["User"] = user;
            context.Items["SessionToken"] = sessionToken;
        }

        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        var publicPaths = new[]
        {
            "/login",
            "/logout",
            "/access-denied",
            "/_blazor",
            "/_framework",
            "/css",
            "/js",
            "/favicon.ico",
            "/healthz"
        };

        return publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetSessionToken(HttpContext context)
    {
        // Try to get from cookie first
        if (context.Request.Cookies.TryGetValue("MPM.Auth", out var cookieToken))
        {
            return cookieToken;
        }

        // Try to get from Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..];
        }

        return null;
    }

    private static async Task RedirectToLogin(HttpContext context)
    {
        if (context.Request.Headers["Accept"].ToString().Contains("text/html"))
        {
            context.Response.Redirect("/login");
        }
        else
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
        }
    }
}