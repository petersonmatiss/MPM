using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services;

namespace MPM.Tests;

public class SupplierQuoteServiceTests
{
    private MpmDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new MpmDbContext(options)
        {
            TenantId = "test-tenant"
        };

        return context;
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateSupplierQuote()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new SupplierQuoteService(context);

        // Create test data
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            Currency = "EUR",
            IsActive = true
        };
        context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S235",
            Dimension = "100x50x5",
            ProfileType = "Angle",
            UnitWeight = 3.77m
        };
        context.Materials.Add(material);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-001",
            SupplierId = supplier.Id,
            Supplier = supplier,
            OrderDate = DateTime.UtcNow,
            Currency = "EUR"
        };
        context.PurchaseOrders.Add(purchaseOrder);

        var purchaseOrderLine = new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrder = purchaseOrder,
            MaterialId = material.Id,
            Material = material,
            Quantity = 100,
            UnitOfMeasure = "kg",
            UnitPrice = 1.50m
        };
        context.PurchaseOrderLines.Add(purchaseOrderLine);

        await context.SaveChangesAsync();

        var supplierQuote = new SupplierQuote
        {
            PurchaseOrderLineId = purchaseOrderLine.Id,
            SupplierId = supplier.Id,
            Price = 1.45m,
            Currency = "EUR",
            ValidityDate = DateTime.Today.AddDays(30),
            LeadTimeDays = 14,
            Notes = "Test quote"
        };

        // Act
        var result = await service.CreateAsync(supplierQuote);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(purchaseOrderLine.Id, result.PurchaseOrderLineId);
        Assert.Equal(supplier.Id, result.SupplierId);
        Assert.Equal(1.45m, result.Price);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal(14, result.LeadTimeDays);
        Assert.Equal("Test quote", result.Notes);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSupplierQuote()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new SupplierQuoteService(context);

        // Create test data
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            Currency = "EUR",
            IsActive = true
        };
        context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S235",
            Dimension = "100x50x5",
            ProfileType = "Angle",
            UnitWeight = 3.77m
        };
        context.Materials.Add(material);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-001",
            SupplierId = supplier.Id,
            Supplier = supplier,
            OrderDate = DateTime.UtcNow,
            Currency = "EUR"
        };
        context.PurchaseOrders.Add(purchaseOrder);

        var purchaseOrderLine = new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrder = purchaseOrder,
            MaterialId = material.Id,
            Material = material,
            Quantity = 100,
            UnitOfMeasure = "kg",
            UnitPrice = 1.50m
        };
        context.PurchaseOrderLines.Add(purchaseOrderLine);

        var supplierQuote = new SupplierQuote
        {
            PurchaseOrderLineId = purchaseOrderLine.Id,
            SupplierId = supplier.Id,
            Price = 1.45m,
            Currency = "EUR",
            ValidityDate = DateTime.Today.AddDays(30),
            LeadTimeDays = 14,
            Notes = "Test quote"
        };
        context.SupplierQuotes.Add(supplierQuote);

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByIdAsync(purchaseOrderLine.Id, supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(purchaseOrderLine.Id, result.PurchaseOrderLineId);
        Assert.Equal(supplier.Id, result.SupplierId);
        Assert.Equal(1.45m, result.Price);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal(14, result.LeadTimeDays);
        Assert.Equal("Test quote", result.Notes);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateSupplierQuote()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new SupplierQuoteService(context);

        // Create test data
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            Currency = "EUR",
            IsActive = true
        };
        context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S235",
            Dimension = "100x50x5",
            ProfileType = "Angle",
            UnitWeight = 3.77m
        };
        context.Materials.Add(material);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-001",
            SupplierId = supplier.Id,
            Supplier = supplier,
            OrderDate = DateTime.UtcNow,
            Currency = "EUR"
        };
        context.PurchaseOrders.Add(purchaseOrder);

        var purchaseOrderLine = new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrder = purchaseOrder,
            MaterialId = material.Id,
            Material = material,
            Quantity = 100,
            UnitOfMeasure = "kg",
            UnitPrice = 1.50m
        };
        context.PurchaseOrderLines.Add(purchaseOrderLine);

        var supplierQuote = new SupplierQuote
        {
            PurchaseOrderLineId = purchaseOrderLine.Id,
            SupplierId = supplier.Id,
            Price = 1.45m,
            Currency = "EUR",
            ValidityDate = DateTime.Today.AddDays(30),
            LeadTimeDays = 14,
            Notes = "Test quote"
        };
        context.SupplierQuotes.Add(supplierQuote);

        await context.SaveChangesAsync();

        // Act
        supplierQuote.Price = 1.35m;
        supplierQuote.LeadTimeDays = 21;
        supplierQuote.Notes = "Updated quote";
        
        var result = await service.UpdateAsync(supplierQuote);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1.35m, result.Price);
        Assert.Equal(21, result.LeadTimeDays);
        Assert.Equal("Updated quote", result.Notes);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteSupplierQuote()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new SupplierQuoteService(context);

        // Create test data
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            Currency = "EUR",
            IsActive = true
        };
        context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S235",
            Dimension = "100x50x5",
            ProfileType = "Angle",
            UnitWeight = 3.77m
        };
        context.Materials.Add(material);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-001",
            SupplierId = supplier.Id,
            Supplier = supplier,
            OrderDate = DateTime.UtcNow,
            Currency = "EUR"
        };
        context.PurchaseOrders.Add(purchaseOrder);

        var purchaseOrderLine = new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrder = purchaseOrder,
            MaterialId = material.Id,
            Material = material,
            Quantity = 100,
            UnitOfMeasure = "kg",
            UnitPrice = 1.50m
        };
        context.PurchaseOrderLines.Add(purchaseOrderLine);

        var supplierQuote = new SupplierQuote
        {
            PurchaseOrderLineId = purchaseOrderLine.Id,
            SupplierId = supplier.Id,
            Price = 1.45m,
            Currency = "EUR",
            ValidityDate = DateTime.Today.AddDays(30),
            LeadTimeDays = 14,
            Notes = "Test quote"
        };
        context.SupplierQuotes.Add(supplierQuote);

        await context.SaveChangesAsync();

        // Act
        await service.DeleteAsync(purchaseOrderLine.Id, supplier.Id);

        // Assert
        var deletedQuote = await service.GetByIdAsync(purchaseOrderLine.Id, supplier.Id);
        Assert.Null(deletedQuote);
    }
}