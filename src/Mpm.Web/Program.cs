using Mpm.Web.Components;
using Mpm.Services;
using Mpm.Data;
using Mpm.Data.Services;
using Mpm.Domain.Entities;
using MudBlazor.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Azure.Identity;
using Mpm.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault if in Production
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Vault"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add HTTP context accessor for tenant resolution
builder.Services.AddHttpContextAccessor();

// Add tenant service
builder.Services.AddScoped<ITenantService, TenantService>();

// Add DbContext
builder.Services.AddDbContext<MpmDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Use in-memory database for development/testing
        options.UseInMemoryDatabase("MpmTestDb");
    }
    else
    {
        // In production, get connection string from Key Vault or configuration
        var connectionString = builder.Configuration["MPMSQL"] ?? builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlServer(connectionString,
            b => b.MigrationsAssembly("Mpm.Api"));
    }
});

// Add MPM services
builder.Services.AddMpmServices();

// Configure authentication
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

var app = builder.Build();

// Seed some test data for development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MpmDbContext>();
    context.TenantId = "test-tenant";
    
    // Add profile types and steel grades
    if (!context.ProfileTypes.Any())
    {
        var profileTypes = new List<ProfileType>
        {
            new ProfileType
            {
                Code = "HEB",
                Name = "HEB Beam",
                Category = "Beam",
                Description = "European H-beam profile",
                StandardWeight = 106.0m,
                DimensionFormat = "HxWxT",
                IsActive = true
            },
            new ProfileType
            {
                Code = "IPE",
                Name = "IPE Beam",
                Category = "Beam",
                Description = "European I-beam profile",
                StandardWeight = 30.7m,
                DimensionFormat = "HxWxT",
                IsActive = true
            }
        };
        
        context.ProfileTypes.AddRange(profileTypes);
        context.SaveChanges();
    }
    
    if (!context.SteelGrades.Any())
    {
        var steelGrades = new List<SteelGrade>
        {
            new SteelGrade
            {
                Code = "S235",
                Name = "S235JR",
                Standard = "EN 10025",
                Description = "Non-alloy structural steel",
                DensityKgPerM3 = 7850,
                YieldStrengthMPa = 235,
                TensileStrengthMPa = 360,
                IsActive = true
            },
            new SteelGrade
            {
                Code = "S355",
                Name = "S355JR",
                Standard = "EN 10025",
                Description = "Non-alloy structural steel",
                DensityKgPerM3 = 7850,
                YieldStrengthMPa = 355,
                TensileStrengthMPa = 510,
                IsActive = true
            }
        };
        
        context.SteelGrades.AddRange(steelGrades);
        context.SaveChanges();
    }
    
    // Add some test profiles
    if (!context.Profiles.Any())
    {
        var hebType = context.ProfileTypes.First(pt => pt.Code == "HEB");
        var ipeType = context.ProfileTypes.First(pt => pt.Code == "IPE");
        var s355Grade = context.SteelGrades.First(sg => sg.Code == "S355");
        var s235Grade = context.SteelGrades.First(sg => sg.Code == "S235");
        
        var profiles = new List<Profile>
        {
            new Profile
            {
                LotId = "A15",
                SteelGradeId = s355Grade.Id,
                ProfileTypeId = hebType.Id,
                Dimension = "200x200x15",
                LengthMm = 12000,
                AvailableLengthMm = 12000,
                Weight = 1272.0m,
                HeatNumber = "H987654",
                SupplierName = "Test Supplier Ltd",
                InvoiceNumber = "INV-001",
                UnitPrice = 50.00m,
                ArrivalDate = DateTime.UtcNow.AddDays(-8),
                IsReserved = false
            },
            new Profile
            {
                LotId = "B3",
                SteelGradeId = s235Grade.Id,
                ProfileTypeId = ipeType.Id,
                Dimension = "200x100x8.5",
                LengthMm = 10000,
                AvailableLengthMm = 6000,
                Weight = 307.0m,
                HeatNumber = "H456789",
                SupplierName = "Test Supplier Ltd",
                InvoiceNumber = "INV-001",
                UnitPrice = 45.00m,
                ArrivalDate = DateTime.UtcNow.AddDays(-12),
                IsReserved = true
            }
        };
        
        context.Profiles.AddRange(profiles);
        context.SaveChanges();
    }
    
    // Create default admin user for development
    if (!context.Users.Any())
    {
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        await authService.CreateUserAsync(
            username: "admin",
            email: "admin@mpm.local",
            firstName: "System",
            lastName: "Administrator",
            password: "Admin123!"
        );
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Add custom authentication middleware
app.UseMiddleware<AuthenticationMiddleware>();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
