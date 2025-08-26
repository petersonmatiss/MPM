using Mpm.Web.Components;
using Mpm.Services;
using Mpm.Data.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add HTTP context accessor for tenant resolution
builder.Services.AddHttpContextAccessor();

// Add MPM services and database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Server=(localdb)\\mssqllocaldb;Database=MpmDb;Trusted_Connection=true;MultipleActiveResultSets=true";

builder.Services.AddMpmDatabase(connectionString);
builder.Services.AddMpmServices();

// Add Identity services
//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
//    .AddEntityFrameworkStores<MpmDbContext>();

var app = builder.Build();

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

//app.UseAuthentication();
//app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
