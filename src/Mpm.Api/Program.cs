using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Services;
using Mpm.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Add DbContext
builder.Services.AddDbContext<MpmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("Mpm.Api")));

// Add MPM services
builder.Services.AddMpmServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Health check endpoint
app.MapHealthChecks("/healthz");

// Suppliers API endpoints
app.MapGet("/api/suppliers", async (ISupplierService supplierService) =>
{
    var suppliers = await supplierService.GetAllAsync();
    return Results.Ok(suppliers);
})
.WithName("GetSuppliers")
.WithOpenApi();

app.MapGet("/api/suppliers/{id}", async (int id, ISupplierService supplierService) =>
{
    var supplier = await supplierService.GetByIdAsync(id);
    return supplier is not null ? Results.Ok(supplier) : Results.NotFound();
})
.WithName("GetSupplier")
.WithOpenApi();

app.MapPost("/api/suppliers", async (Supplier supplier, ISupplierService supplierService) =>
{
    try
    {
        var created = await supplierService.CreateAsync(supplier);
        return Results.Created($"/api/suppliers/{created.Id}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateSupplier")
.WithOpenApi();

app.MapPut("/api/suppliers/{id}", async (int id, Supplier supplier, ISupplierService supplierService) =>
{
    if (id != supplier.Id)
        return Results.BadRequest(new { error = "ID mismatch" });

    try
    {
        var updated = await supplierService.UpdateAsync(supplier);
        return Results.Ok(updated);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateSupplier")
.WithOpenApi();

app.MapDelete("/api/suppliers/{id}", async (int id, ISupplierService supplierService) =>
{
    await supplierService.DeleteAsync(id);
    return Results.NoContent();
})
.WithName("DeleteSupplier")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
