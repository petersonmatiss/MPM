using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain;
using Mpm.Domain.Entities;
using Mpm.Services;

namespace MPM.Tests;

/// <summary>
/// Integration test to demonstrate the acceptance criteria are met
/// </summary>
public class PriceRequestAcceptanceCriteriaTests
{
    private MpmDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new MpmDbContext(options);
        context.TenantId = "test-tenant";
        return context;
    }

    [Fact]
    public async Task AcceptanceCriteria_CanPersistPriceRequestWithMultipleLinesOfMixedTypes()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        // Create steel grades (following the requirement to seed 2-3 steel grades)
        var steelGrades = new List<SteelGrade>
        {
            new SteelGrade { Code = "S235", Name = "S235JR", Standard = "EN 10025", DensityKgPerM3 = 7850, YieldStrengthMPa = 235, TensileStrengthMPa = 360, IsActive = true },
            new SteelGrade { Code = "S355", Name = "S355JR", Standard = "EN 10025", DensityKgPerM3 = 7850, YieldStrengthMPa = 355, TensileStrengthMPa = 510, IsActive = true },
            new SteelGrade { Code = "S275", Name = "S275JR", Standard = "EN 10025", DensityKgPerM3 = 7850, YieldStrengthMPa = 275, TensileStrengthMPa = 430, IsActive = true }
        };
        
        context.SteelGrades.AddRange(steelGrades);
        await context.SaveChangesAsync();

        // Create suppliers
        var suppliers = new List<Supplier>
        {
            new Supplier { Name = "Steel Supplier A", VatNumber = "LV11111111111", Email = "a@supplier.com", IsActive = true },
            new Supplier { Name = "Steel Supplier B", VatNumber = "LV22222222222", Email = "b@supplier.com", IsActive = true }
        };
        
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();

        // Act - Create price request with N lines of mixed types
        var priceRequest = new PriceRequest
        {
            Description = "Mixed materials for Project Alpha",
            RequestedBy = "John Buyer",
            Notes = "Urgent procurement for Q1 delivery",
            Lines = new List<PriceRequestLine>
            {
                // Sheet material line
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Hot rolled steel sheet for base plates",
                    LengthMm = 6000,      // Dimensions in millimeters (integers)
                    WidthMm = 2000,
                    ThicknessMm = 15,
                    Pieces = 10,
                    SteelGradeId = steelGrades[0].Id, // FK to existing SteelGrade
                    Notes = "Mill certificate required"
                },
                // Profile material line
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Profile,
                    Description = "HEB 200 structural beams",
                    Dimension = "200x200x15", // Following inventory dimension conventions
                    TotalLength = 120000, // Total length in millimeters
                    Pieces = 10,          // Number of pieces
                    SteelGradeId = steelGrades[1].Id, // FK to existing SteelGrade
                    Notes = "Standard 12m lengths preferred"
                },
                // Another sheet with different steel grade
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Weathering steel sheet for cladding",
                    LengthMm = 4000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 25,
                    SteelGradeId = steelGrades[2].Id,
                    Notes = "Corten finish required"
                }
            }
        };

        var result = await service.CreateAsync(priceRequest);

        // Assert - Can persist PR with N lines of mixed types
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(3, result.Lines.Count);
        
        // Verify mixed types are supported
        Assert.Contains(result.Lines, l => l.MaterialType == MaterialType.Sheet);
        Assert.Contains(result.Lines, l => l.MaterialType == MaterialType.Profile);
        
        // Verify steel grade FKs are properly set
        Assert.All(result.Lines, line => Assert.True(line.SteelGradeId > 0));
        
        // Verify dimensions follow millimeter integer constraints
        var sheetLines = result.Lines.Where(l => l.MaterialType == MaterialType.Sheet);
        Assert.All(sheetLines, line => 
        {
            Assert.True(line.LengthMm > 0);
            Assert.True(line.WidthMm > 0);
            Assert.True(line.ThicknessMm > 0);
        });
        
        var profileLines = result.Lines.Where(l => l.MaterialType == MaterialType.Profile);
        Assert.All(profileLines, line => 
        {
            Assert.False(string.IsNullOrEmpty(line.Dimension));
            Assert.True(line.TotalLength > 0);
            Assert.True(line.Pieces > 0);
        });

        // Add suppliers to the request
        await service.AddSupplierAsync(result.Id, suppliers[0].Id);
        await service.AddSupplierAsync(result.Id, suppliers[1].Id);
        
        var withSuppliers = await service.GetByIdAsync(result.Id);
        Assert.Equal(2, withSuppliers!.Suppliers.Count);
    }

    [Fact]
    public async Task AcceptanceCriteria_ServerValidationEnforcesRequiredFieldsByType()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);

        // Act & Assert - Sheet validation
        var sheetRequestMissingDimensions = new PriceRequest
        {
            Description = "Invalid sheet request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    // Missing required LengthMm, WidthMm, ThicknessMm
                    Pieces = 1
                }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(sheetRequestMissingDimensions));

        // Act & Assert - Profile validation
        var profileRequestMissingFields = new PriceRequest
        {
            Description = "Invalid profile request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Profile,
                    Description = "HEB beam",
                    // Missing required Dimension, TotalLength, Pieces
                }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(profileRequestMissingFields));
    }

    [Fact]
    public async Task AcceptanceCriteria_StatusTransitionsEnforcedByDomainRules()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Status transition test",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Test sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 10,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);
        Assert.Equal(PriceRequestStatus.Draft, created.Status);

        // Valid transitions
        var sent = await service.ChangeStatusAsync(created.Id, PriceRequestStatus.Sent);
        Assert.Equal(PriceRequestStatus.Sent, sent.Status);
        Assert.NotNull(sent.SentDate);

        var collecting = await service.ChangeStatusAsync(created.Id, PriceRequestStatus.Collecting);
        Assert.Equal(PriceRequestStatus.Collecting, collecting.Status);

        var completed = await service.ChangeStatusAsync(created.Id, PriceRequestStatus.Completed);
        Assert.Equal(PriceRequestStatus.Completed, completed.Status);

        // Can always cancel
        var canceled = await service.ChangeStatusAsync(created.Id, PriceRequestStatus.Canceled);
        Assert.Equal(PriceRequestStatus.Canceled, canceled.Status);

        // Invalid transition - from Canceled (terminal state)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangeStatusAsync(created.Id, PriceRequestStatus.Draft));

        // Test invalid transition from Draft directly to Completed
        var anotherRequest = new PriceRequest
        {
            Description = "Another test",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Test sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 10,
                    Pieces = 1
                }
            }
        };

        var another = await service.CreateAsync(anotherRequest);
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangeStatusAsync(another.Id, PriceRequestStatus.Completed));
    }
}