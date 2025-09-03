using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Services;
using Mpm.Services.DTOs;
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
                    StandardWeight = 106.0m, // kg/m for HEB 200
                    DimensionFormat = "HxWxT",
                    IsActive = true
                },
                new ProfileType
                {
                    Code = "IPE",
                    Name = "IPE Beam",
                    Category = "Beam",
                    Description = "European I-beam profile",
                    StandardWeight = 30.7m, // kg/m for IPE 200
                    DimensionFormat = "HxWxT",
                    IsActive = true
                },
                new ProfileType
                {
                    Code = "UPN",
                    Name = "UPN Channel",
                    Category = "Channel",
                    Description = "European channel profile",
                    StandardWeight = 25.3m, // kg/m for UPN 200
                    DimensionFormat = "HxWxT",
                    IsActive = true
                },
                new ProfileType
                {
                    Code = "L",
                    Name = "Equal Angle",
                    Category = "Angle",
                    Description = "Equal angle profile",
                    StandardWeight = 9.4m, // kg/m for L 100x100x10
                    DimensionFormat = "LxLxT",
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
                },
                new SteelGrade
                {
                    Code = "S275",
                    Name = "S275JR",
                    Standard = "EN 10025",
                    Description = "Non-alloy structural steel",
                    DensityKgPerM3 = 7850,
                    YieldStrengthMPa = 275,
                    TensileStrengthMPa = 430,
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
                    LengthMm = 12000, // Total length of all pieces
                    PieceLength = 12000, // Each piece is 12000mm long
                    PiecesAvailable = 1, // 1 piece available
                    AvailableLengthMm = 12000, // Legacy field
                    Weight = 1272.0m, // 106 kg/m * 12m
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
                    LengthMm = 20000, // Total length: 2 pieces of 10000mm each
                    PieceLength = 10000, // Each piece is 10000mm long
                    PiecesAvailable = 2, // 2 pieces available initially (later we'll simulate usage)
                    AvailableLengthMm = 6000, // Legacy field - shows 4m has been used (will be updated)
                    Weight = 614.0m, // 30.7 kg/m * 20m
                    HeatNumber = "H456789",
                    SupplierName = "Test Supplier Ltd",
                    InvoiceNumber = "INV-001",
                    UnitPrice = 45.00m,
                    ArrivalDate = DateTime.UtcNow.AddDays(-12),
                    IsReserved = true
                },
                new Profile
                {
                    LotId = "C1",
                    SteelGradeId = s355Grade.Id,
                    ProfileTypeId = hebType.Id,
                    Dimension = "200x200x15",
                    LengthMm = 24000, // Total length: 3 pieces of 8000mm each
                    PieceLength = 8000, // Each piece is 8000mm long
                    PiecesAvailable = 3, // 3 pieces available
                    AvailableLengthMm = 24000, // Legacy field
                    Weight = 2544.0m, // 106 kg/m * 24m
                    HeatNumber = "H123789",
                    SupplierName = "Test Supplier Ltd",
                    InvoiceNumber = "INV-001",
                    UnitPrice = 52.00m,
                    ArrivalDate = DateTime.UtcNow.AddDays(-6),
                    IsReserved = false
                }
            };
            
            context.Profiles.AddRange(profiles);
            context.SaveChanges();
            
            // Simulate usage of profile B3 to show piece-based tracking
            var profileB3 = profiles.FirstOrDefault(p => p.LotId == "B3");
            if (profileB3 != null)
            {
                // Reduce available pieces from 2 to 1 (one 10m piece was used)
                profileB3.PiecesAvailable = 1;
                profileB3.AvailableLengthMm = profileB3.PiecesAvailable * profileB3.PieceLength;
                
                // Create a usage record for the used piece
                var usage = new ProfileUsage
                {
                    ProfileId = profileB3.Id,
                    UsageDate = DateTime.UtcNow.AddDays(-3),
                    UsedBy = "Jane Smith",
                    UsedPieceLength = 10000,
                    PiecesUsed = 1,
                    RemnantFlag = true,
                    RemnantPieceLength = 4000,
                    RemnantPiecesCreated = 1,
                    Notes = "Cut for beam project - created 4m remnant"
                };
                context.ProfileUsages.Add(usage);
                
                // Create a remnant from the used piece
                var remnant = new ProfileRemnant
                {
                    ProfileId = profileB3.Id,
                    RemnantId = "B3-4000-r001",
                    LengthMm = 4000,
                    PieceLength = 4000,
                    PiecesAvailable = 1,
                    Weight = 122.8m, // 30.7 kg/m * 4m
                    IsUsable = true,
                    IsUsed = false,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    Notes = "Created from usage: Cut for beam project - created 4m remnant"
                };
                context.ProfileRemnants.Add(remnant);
                
                context.SaveChanges();
            }
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

// Supplier Quotes API endpoints
app.MapGet("/api/supplier-quotes", async (ISupplierQuoteService supplierQuoteService) =>
{
    var quotes = await supplierQuoteService.GetAllAsync();
    return Results.Ok(quotes);
})
.WithName("GetSupplierQuotes")
.WithOpenApi();

app.MapGet("/api/supplier-quotes/purchase-order-line/{purchaseOrderLineId}", async (int purchaseOrderLineId, ISupplierQuoteService supplierQuoteService) =>
{
    var quotes = await supplierQuoteService.GetByPurchaseOrderLineAsync(purchaseOrderLineId);
    return Results.Ok(quotes);
})
.WithName("GetSupplierQuotesByPurchaseOrderLine")
.WithOpenApi();

app.MapGet("/api/supplier-quotes/supplier/{supplierId}", async (int supplierId, ISupplierQuoteService supplierQuoteService) =>
{
    var quotes = await supplierQuoteService.GetBySupplierAsync(supplierId);
    return Results.Ok(quotes);
})
.WithName("GetSupplierQuotesBySupplier")
.WithOpenApi();

app.MapGet("/api/supplier-quotes/{purchaseOrderLineId}/{supplierId}", async (int purchaseOrderLineId, int supplierId, ISupplierQuoteService supplierQuoteService) =>
{
    var quote = await supplierQuoteService.GetByIdAsync(purchaseOrderLineId, supplierId);
    return quote is not null ? Results.Ok(quote) : Results.NotFound();
})
.WithName("GetSupplierQuote")
.WithOpenApi();

app.MapPost("/api/supplier-quotes", async (SupplierQuote quote, ISupplierQuoteService supplierQuoteService) =>
{
    try
    {
        var created = await supplierQuoteService.CreateAsync(quote);
        return Results.Created($"/api/supplier-quotes/{created.PurchaseOrderLineId}/{created.SupplierId}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateSupplierQuote")
.WithOpenApi();

app.MapPut("/api/supplier-quotes/{purchaseOrderLineId}/{supplierId}", async (int purchaseOrderLineId, int supplierId, SupplierQuote quote, ISupplierQuoteService supplierQuoteService) =>
{
    if (purchaseOrderLineId != quote.PurchaseOrderLineId || supplierId != quote.SupplierId)
        return Results.BadRequest(new { error = "ID mismatch" });

    try
    {
        var updated = await supplierQuoteService.UpdateAsync(quote);
        return Results.Ok(updated);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateSupplierQuote")
.WithOpenApi();

app.MapDelete("/api/supplier-quotes/{purchaseOrderLineId}/{supplierId}", async (int purchaseOrderLineId, int supplierId, ISupplierQuoteService supplierQuoteService) =>
{
    await supplierQuoteService.DeleteAsync(purchaseOrderLineId, supplierId);
    return Results.NoContent();
})
.WithName("DeleteSupplierQuote")
.WithOpenApi();

app.MapPost("/api/supplier-quotes/import-csv", async (IFormFile csvFile, ISupplierQuoteService supplierQuoteService) =>
{
    if (csvFile == null || csvFile.Length == 0)
        return Results.BadRequest(new { error = "CSV file is required" });

    try
    {
        using var stream = csvFile.OpenReadStream();
        var quotes = await supplierQuoteService.ImportFromCsvAsync(stream);
        return Results.Ok(new { message = $"Successfully imported {quotes.Count()} quotes", quotes });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Import failed: {ex.Message}" });
    }
})
.WithName("ImportSupplierQuotesFromCsv")
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

// Profiles API endpoints
app.MapGet("/api/profiles", async (IProfileService profileService, int? profileTypeId, int? steelGradeId, string? searchFilter) =>
{
    var profiles = await profileService.GetAllAsync(profileTypeId, steelGradeId, searchFilter);
    return Results.Ok(profiles);
})
.WithName("GetProfiles")
.WithOpenApi();

app.MapGet("/api/profiles/available", async (IProfileService profileService, int? profileTypeId, int? steelGradeId) =>
{
    var availableProfiles = await profileService.GetAvailableProfilesAsync(profileTypeId, steelGradeId);
    return Results.Ok(availableProfiles);
})
.WithName("GetAvailableProfiles")
.WithOpenApi();

app.MapGet("/api/profiles/{id}", async (int id, IProfileService profileService) =>
{
    var profile = await profileService.GetByIdAsync(id);
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
})
.WithName("GetProfile")
.WithOpenApi();

app.MapGet("/api/profiles/lot/{lotId}", async (string lotId, IProfileService profileService) =>
{
    var profile = await profileService.GetByLotIdAsync(lotId);
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
})
.WithName("GetProfileByLotId")
.WithOpenApi();

app.MapGet("/api/profiles/{id}/remnants", async (int id, IProfileService profileService) =>
{
    var remnants = await profileService.GetRemnantsAsync(id);
    return Results.Ok(remnants);
})
.WithName("GetProfileRemnants")
.WithOpenApi();

app.MapGet("/api/remnants", async (IProfileService profileService, bool? availableOnly) =>
{
    var remnants = await profileService.GetAllRemnantsAsync(availableOnly ?? true);
    return Results.Ok(remnants);
})
.WithName("GetAllRemnants")
.WithOpenApi();

app.MapGet("/api/remnants/{id}", async (int id, IProfileService profileService) =>
{
    var remnant = await profileService.GetRemnantByIdAsync(id);
    return remnant is not null ? Results.Ok(remnant) : Results.NotFound();
})
.WithName("GetRemnant")
.WithOpenApi();

app.MapPost("/api/profiles/{lotId}/use", async (string lotId, ProfileUsageRequest request, IProfileService profileService) =>
{
    try
    {
        var usage = await profileService.UseProfileAsync(lotId, request);
        return Results.Ok(usage);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UseProfile")
.WithOpenApi();

app.MapPost("/api/remnants/{id}/use", async (int id, RemnantUsageRequest request, IProfileService profileService) =>
{
    try
    {
        var usage = await profileService.UseRemnantAsync(id, request);
        return Results.Ok(usage);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UseRemnant")
.WithOpenApi();

app.MapPost("/api/profiles", async (Profile profile, IProfileService profileService) =>
{
    try
    {
        var created = await profileService.CreateAsync(profile);
        return Results.Created($"/api/profiles/{created.Id}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateProfile")
.WithOpenApi();

app.MapPut("/api/profiles/{id}", async (int id, Profile profile, IProfileService profileService) =>
{
    if (id != profile.Id)
        return Results.BadRequest(new { error = "ID mismatch" });

    try
    {
        var updated = await profileService.UpdateAsync(profile);
        return Results.Ok(updated);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateProfile")
.WithOpenApi();

app.MapDelete("/api/profiles/{id}", async (int id, IProfileService profileService) =>
{
    try
    {
        await profileService.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("DeleteProfile")
.WithOpenApi();

// Lookup APIs for ProfileType and SteelGrade
app.MapGet("/api/profile-types", async (IProfileTypeService profileTypeService) =>
{
    var profileTypes = await profileTypeService.GetAllActiveAsync();
    return Results.Ok(profileTypes);
})
.WithName("GetProfileTypes")
.WithOpenApi();

app.MapGet("/api/steel-grades", async (ISteelGradeService steelGradeService) =>
{
    var steelGrades = await steelGradeService.GetAllActiveAsync();
    return Results.Ok(steelGrades);
})
.WithName("GetSteelGrades")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
