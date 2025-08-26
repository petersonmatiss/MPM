using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Services;
using Mpm.Domain.Entities;
using Mpm.Domain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Add required services
builder.Services.AddHttpContextAccessor();

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
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("Mpm.Api"));
    }
});

// Add MPM services
builder.Services.AddMpmServices();

var app = builder.Build();

// Seed some test data for development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MpmDbContext>();
    context.TenantId = "test-tenant";
    
    if (!context.Suppliers.Any())
    {
        var supplier = new Supplier
        {
            Name = "Test Supplier Ltd",
            VatNumber = "LV12345678901",
            Email = "test@supplier.com",
            Currency = Constants.Currency.EUR,
            IsActive = true
        };
        context.Suppliers.Add(supplier);
        context.SaveChanges();
        
        var invoice = new Invoice
        {
            Number = "INV-001",
            SupplierId = supplier.Id,
            InvoiceDate = DateTime.UtcNow.AddDays(-5),
            Currency = Constants.Currency.EUR,
            SubTotal = 1000.00m,
            TaxAmount = 210.00m,
            TotalAmount = 1210.00m,
            Notes = "Test invoice for demonstration",
            Lines = new List<InvoiceLine>
            {
                new InvoiceLine
                {
                    Description = "Steel Profile HEB 200",
                    ItemType = "Profile",
                    Quantity = 10.5m,
                    UnitOfMeasure = "m",
                    UnitPrice = 50.00m,
                    TotalPrice = 525.00m
                },
                new InvoiceLine
                {
                    Description = "Steel Sheet S355 10mm",
                    ItemType = "Sheet",
                    Quantity = 2.5m,
                    UnitOfMeasure = "m²",
                    UnitPrice = 190.00m,
                    TotalPrice = 475.00m
                }
            }
        };
        context.Invoices.Add(invoice);
        context.SaveChanges();

        // Add some test sheets
        if (!context.Sheets.Any())
        {
            var sheets = new List<Sheet>
            {
                new Sheet
                {
                    SheetId = "S001",
                    Grade = "S355",
                    LengthMm = 6000,
                    WidthMm = 3000,
                    ThicknessMm = 10,
                    Weight = 1413.0m,
                    HeatNumber = "H123456",
                    SupplierName = "Test Supplier Ltd",
                    InvoiceNumber = "INV-001",
                    UnitPrice = 190.00m,
                    ArrivalDate = DateTime.UtcNow.AddDays(-10),
                    IsReserved = false,
                    IsUsed = false
                },
                new Sheet
                {
                    SheetId = "S002",
                    Grade = "S235",
                    LengthMm = 4000,
                    WidthMm = 2000,
                    ThicknessMm = 8,
                    Weight = 502.4m,
                    HeatNumber = "H789012",
                    SupplierName = "Test Supplier Ltd",
                    InvoiceNumber = "INV-001",
                    UnitPrice = 170.00m,
                    ArrivalDate = DateTime.UtcNow.AddDays(-5),
                    IsReserved = true,
                    IsUsed = false
                },
                new Sheet
                {
                    SheetId = "S003",
                    Grade = "S355",
                    LengthMm = 5000,
                    WidthMm = 2500,
                    ThicknessMm = 12,
                    Weight = 1177.5m,
                    HeatNumber = "H345678",
                    SupplierName = "Test Supplier Ltd",
                    InvoiceNumber = "INV-001",
                    UnitPrice = 200.00m,
                    ArrivalDate = DateTime.UtcNow.AddDays(-15),
                    IsReserved = false,
                    IsUsed = true
                }
            };
            
            context.Sheets.AddRange(sheets);
            context.SaveChanges();

            // Add a sheet usage for the used sheet to create a remnant example
            var sheetUsage = new SheetUsage
            {
                SheetId = sheets[2].Id, // S003
                ProjectId = null,
                NestId = "NEST001",
                UsageDate = DateTime.UtcNow.AddDays(-3),
                UsedBy = "John Doe",
                AreaUsed = 8000000, // 8 m² in mm²
                UsedLengthMm = 4000,
                UsedWidthMm = 2000,
                Notes = "Cut for test project",
                GeneratedRemnants = true,
                RemnantDetails = "[{\"length\": 1000, \"width\": 2500}, {\"length\": 5000, \"width\": 500}]"
            };
            
            context.SheetUsages.Add(sheetUsage);
            context.SaveChanges();
        }
    }
}

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

// Invoices API endpoints
app.MapGet("/api/invoices", async (IInvoiceService invoiceService, int? supplierId, DateTime? fromDate, DateTime? toDate) =>
{
    var invoices = await invoiceService.GetAllAsync(supplierId, fromDate, toDate);
    return Results.Ok(invoices);
})
.WithName("GetInvoices")
.WithOpenApi();

app.MapGet("/api/invoices/{id}", async (int id, IInvoiceService invoiceService) =>
{
    var invoice = await invoiceService.GetByIdAsync(id);
    return invoice is not null ? Results.Ok(invoice) : Results.NotFound();
})
.WithName("GetInvoice")
.WithOpenApi();

app.MapPost("/api/invoices", async (Invoice invoice, IInvoiceService invoiceService) =>
{
    try
    {
        var created = await invoiceService.CreateAsync(invoice);
        return Results.Created($"/api/invoices/{created.Id}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateInvoice")
.WithOpenApi();

app.MapPut("/api/invoices/{id}", async (int id, Invoice invoice, IInvoiceService invoiceService) =>
{
    if (id != invoice.Id)
        return Results.BadRequest(new { error = "ID mismatch" });

    try
    {
        var updated = await invoiceService.UpdateAsync(invoice);
        return Results.Ok(updated);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateInvoice")
.WithOpenApi();

app.MapDelete("/api/invoices/{id}", async (int id, IInvoiceService invoiceService) =>
{
    await invoiceService.DeleteAsync(id);
    return Results.NoContent();
})
.WithName("DeleteInvoice")
.WithOpenApi();

// Sheets API endpoints
app.MapGet("/api/sheets", async (ISheetService sheetService, int? thicknessMm, string? sizeFilter) =>
{
    var sheets = await sheetService.GetAllAsync(thicknessMm, sizeFilter);
    return Results.Ok(sheets);
})
.WithName("GetSheets")
.WithOpenApi();

app.MapGet("/api/sheets/remnants", async (ISheetService sheetService, int? thicknessMm, string? sizeFilter) =>
{
    var remnantSheets = await sheetService.GetRemnantSheetsAsync(thicknessMm, sizeFilter);
    return Results.Ok(remnantSheets);
})
.WithName("GetRemnantSheets")
.WithOpenApi();

app.MapGet("/api/sheets/{id}", async (int id, ISheetService sheetService) =>
{
    var sheet = await sheetService.GetByIdAsync(id);
    return sheet is not null ? Results.Ok(sheet) : Results.NotFound();
})
.WithName("GetSheet")
.WithOpenApi();

app.MapPost("/api/sheets", async (Sheet sheet, ISheetService sheetService) =>
{
    try
    {
        var created = await sheetService.CreateAsync(sheet);
        return Results.Created($"/api/sheets/{created.Id}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateSheet")
.WithOpenApi();

app.MapPut("/api/sheets/{id}", async (int id, Sheet sheet, ISheetService sheetService) =>
{
    if (id != sheet.Id)
        return Results.BadRequest(new { error = "ID mismatch" });

    try
    {
        var updated = await sheetService.UpdateAsync(sheet);
        return Results.Ok(updated);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateSheet")
.WithOpenApi();

app.MapDelete("/api/sheets/{id}", async (int id, ISheetService sheetService) =>
{
    try
    {
        await sheetService.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("DeleteSheet")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
