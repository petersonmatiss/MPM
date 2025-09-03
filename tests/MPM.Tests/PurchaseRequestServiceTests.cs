using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Domain;
using Mpm.Services;

namespace MPM.Tests;

public class PurchaseRequestServiceTests
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

    private async Task<Material> CreateTestMaterial(MpmDbContext context)
    {
        var material = new Material
        {
            Grade = "S235",
            Dimension = "200x100x8.5",
            ProfileType = "IPE",
            UnitWeight = 30.7m,
            Surface = "Hot Rolled",
            Standard = "EN 10025-2",
            Description = "Test Steel Profile"
        };
        context.Materials.Add(material);
        await context.SaveChangesAsync();
        return material;
    }

    private async Task<Supplier> CreateTestSupplier(MpmDbContext context, string name = "Test Supplier Ltd")
    {
        var supplier = new Supplier
        {
            Name = name,
            VatNumber = "LV12345678901",
            Email = "test@supplier.com",
            Currency = Constants.Currency.EUR,
            IsActive = true
        };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    [Fact]
    public async Task CreateAsync_ValidPurchaseRequest_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var material = await CreateTestMaterial(context);
        var service = new PurchaseRequestService(context);
        
        var purchaseRequest = new PurchaseRequest
        {
            Number = "PR-001",
            Description = "Test Purchase Request",
            RequestDate = DateTime.Today,
            RequiredDate = DateTime.Today.AddDays(30),
            Lines = new List<PurchaseRequestLine>
            {
                new PurchaseRequestLine
                {
                    MaterialId = material.Id,
                    Quantity = 10.5m,
                    UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
                    RequiredDate = DateTime.Today.AddDays(30)
                }
            }
        };

        // Act
        var result = await service.CreateAsync(purchaseRequest);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("PR-001", result.Number);
        Assert.Single(result.Lines);
        Assert.Equal(material.Id, result.Lines.First().MaterialId);
    }

    [Fact]
    public async Task SetWinnerForLineAsync_ValidSelection_ShouldSetWinner()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var material = await CreateTestMaterial(context);
        var supplier = await CreateTestSupplier(context);
        var service = new PurchaseRequestService(context);
        
        var purchaseRequest = new PurchaseRequest
        {
            Number = "PR-002",
            Description = "Test PR for Winner Selection",
            Lines = new List<PurchaseRequestLine>
            {
                new PurchaseRequestLine
                {
                    MaterialId = material.Id,
                    Quantity = 5.0m,
                    UnitOfMeasure = Constants.UnitOfMeasure.Kilogram
                }
            }
        };

        var created = await service.CreateAsync(purchaseRequest);
        var line = created.Lines.First();

        // Create a supplier quote and quote line
        var supplierQuote = new SupplierQuote
        {
            PurchaseRequestId = created.Id,
            SupplierId = supplier.Id,
            QuoteNumber = "Q-001",
            QuoteDate = DateTime.Today,
            Currency = Constants.Currency.EUR,
            Lines = new List<SupplierQuoteLine>
            {
                new SupplierQuoteLine
                {
                    PurchaseRequestLineId = line.Id,
                    UnitPrice = 2.50m,
                    TotalPrice = 12.50m,
                    IsAvailable = true
                }
            }
        };

        context.SupplierQuotes.Add(supplierQuote);
        await context.SaveChangesAsync();

        var quoteLine = supplierQuote.Lines.First();

        // Act
        var result = await service.SetWinnerForLineAsync(line.Id, supplier.Id, quoteLine.Id);

        // Assert
        Assert.NotNull(result);
        var updatedLine = result.Lines.First(l => l.Id == line.Id);
        Assert.Equal(supplier.Id, updatedLine.WinnerSupplierId);
        Assert.Equal(quoteLine.Id, updatedLine.WinnerQuoteLineId);
        Assert.NotNull(updatedLine.WinnerSelectedDate);
    }

    [Fact]
    public async Task ValidateCompletionAsync_AllLinesHaveWinners_ShouldReturnTrue()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var material = await CreateTestMaterial(context);
        var supplier = await CreateTestSupplier(context);
        var service = new PurchaseRequestService(context);
        
        var purchaseRequest = new PurchaseRequest
        {
            Number = "PR-003",
            Description = "Test PR for Validation",
            Lines = new List<PurchaseRequestLine>
            {
                new PurchaseRequestLine
                {
                    MaterialId = material.Id,
                    Quantity = 5.0m,
                    UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
                    WinnerSupplierId = supplier.Id
                }
            }
        };

        var created = await service.CreateAsync(purchaseRequest);

        // Act
        var canComplete = await service.ValidateCompletionAsync(created.Id);

        // Assert
        Assert.True(canComplete);
    }

    [Fact]
    public async Task ValidateCompletionAsync_SomeLinesWithoutWinners_ShouldReturnFalse()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var material = await CreateTestMaterial(context);
        var service = new PurchaseRequestService(context);
        
        var purchaseRequest = new PurchaseRequest
        {
            Number = "PR-004",
            Description = "Test PR with Incomplete Selection",
            Lines = new List<PurchaseRequestLine>
            {
                new PurchaseRequestLine
                {
                    MaterialId = material.Id,
                    Quantity = 5.0m,
                    UnitOfMeasure = Constants.UnitOfMeasure.Kilogram
                    // No winner selected
                }
            }
        };

        var created = await service.CreateAsync(purchaseRequest);

        // Act
        var canComplete = await service.ValidateCompletionAsync(created.Id);

        // Assert
        Assert.False(canComplete);
    }
}