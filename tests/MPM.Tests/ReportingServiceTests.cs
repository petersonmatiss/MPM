using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services;
using Mpm.Services.DTOs;
using Xunit;

namespace MPM.Tests;

public class ReportingServiceTests : IDisposable
{
    private readonly MpmDbContext _context;
    private readonly ReportingService _reportingService;

    public ReportingServiceTests()
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MpmDbContext(options);
        _reportingService = new ReportingService(_context);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        var material = new Material
        {
            Grade = "S235",
            Dimension = "200x100x8.5",
            ProfileType = "IPE",
            Description = "Test Material"
        };

        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            IsActive = true
        };

        var inventoryLot = new InventoryLot
        {
            Material = material,
            Quantity = 100.0m,
            UnitPrice = 50.0m,
            HeatNumber = "H123456",
            SupplierName = supplier.Name,
            ArrivalDate = DateTime.UtcNow.AddDays(-30),
            ProfileType = material.ProfileType
        };

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-001",
            Supplier = supplier,
            OrderDate = DateTime.UtcNow.AddDays(-15),
            DeliveryDate = DateTime.UtcNow.AddDays(-5),
            IsConfirmed = true
        };

        var purchaseOrderLine = new PurchaseOrderLine
        {
            PurchaseOrder = purchaseOrder,
            Material = material,
            Quantity = 100.0m,
            UnitPrice = 50.0m
        };

        _context.Materials.Add(material);
        _context.Suppliers.Add(supplier);
        _context.InventoryLots.Add(inventoryLot);
        _context.PurchaseOrders.Add(purchaseOrder);
        _context.PurchaseOrderLines.Add(purchaseOrderLine);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetInventoryReportAsync_ShouldReturnInventoryData()
    {
        // Act
        var result = await _reportingService.GetInventoryReportAsync();

        // Assert
        Assert.NotNull(result);
        var inventoryItems = result.ToList();
        Assert.Single(inventoryItems);
        
        var item = inventoryItems[0];
        Assert.Equal("S235", item.MaterialGrade);
        Assert.Equal("IPE", item.ProfileType);
        Assert.Equal(100.0m, item.TotalQuantity);
        Assert.Equal(5000.0m, item.TotalValue); // 100 * 50
        Assert.Equal("Test Supplier", item.PrimarySupplier);
    }

    [Fact]
    public async Task GetInventoryLevelSummaryAsync_ShouldReturnCorrectSummary()
    {
        // Act
        var result = await _reportingService.GetInventoryLevelSummaryAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000.0m, result.TotalInventoryValue);
        Assert.Equal(1, result.TotalLots);
        Assert.Equal(0, result.ReservedLots);
        Assert.Equal(1, result.UniqueMaterials);
    }

    [Fact]
    public async Task GetPurchaseOrderReportAsync_ShouldReturnPurchaseOrderData()
    {
        // Act
        var result = await _reportingService.GetPurchaseOrderReportAsync();

        // Assert
        Assert.NotNull(result);
        var orders = result.ToList();
        Assert.Single(orders);
        
        var order = orders[0];
        Assert.Equal("PO-001", order.Number);
        Assert.Equal("Test Supplier", order.SupplierName);
        Assert.Equal(5000.0m, order.TotalValue); // 100 * 50
        Assert.True(order.IsConfirmed);
        Assert.False(order.IsDelivered); // No goods receipt notes
    }

    [Fact]
    public async Task GetSupplierPerformanceReportAsync_ShouldReturnSupplierPerformance()
    {
        // Act
        var result = await _reportingService.GetSupplierPerformanceReportAsync();

        // Assert
        Assert.NotNull(result);
        var suppliers = result.ToList();
        Assert.Single(suppliers);
        
        var supplier = suppliers[0];
        Assert.Equal("Test Supplier", supplier.SupplierName);
        Assert.Equal(1, supplier.TotalOrders);
        Assert.Equal(5000.0m, supplier.TotalOrderValue);
        Assert.Equal(50.0m, supplier.AveragePricePerKg);
    }

    [Fact]
    public async Task GetMaterialCostTrendsAsync_ShouldReturnCostTrends()
    {
        // Act
        var result = await _reportingService.GetMaterialCostTrendsAsync();

        // Assert
        Assert.NotNull(result);
        var trends = result.ToList();
        Assert.Single(trends);
        
        var trend = trends[0];
        Assert.Equal("S235", trend.MaterialGrade);
        Assert.Equal("IPE", trend.ProfileType);
        Assert.Equal(50.0m, trend.UnitPrice);
        Assert.Equal("Test Supplier", trend.SupplierName);
        Assert.Equal(100.0m, trend.Quantity);
    }

    [Fact]
    public async Task GetInventoryReportAsync_WithFilterByMaterialGrade_ShouldFilterCorrectly()
    {
        // Arrange
        var filter = new ReportFilterDto { MaterialGrade = "S235" };

        // Act
        var result = await _reportingService.GetInventoryReportAsync(filter);

        // Assert
        Assert.NotNull(result);
        var inventoryItems = result.ToList();
        Assert.Single(inventoryItems);
        Assert.Equal("S235", inventoryItems[0].MaterialGrade);
    }

    [Fact]
    public async Task GetInventoryReportAsync_WithFilterByNonExistentGrade_ShouldReturnEmpty()
    {
        // Arrange
        var filter = new ReportFilterDto { MaterialGrade = "NonExistent" };

        // Act
        var result = await _reportingService.GetInventoryReportAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}