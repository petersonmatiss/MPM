using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services;
using Mpm.Services.DTOs;

namespace MPM.Tests;

public class MaterialReceiptServiceTests : IDisposable
{
    private readonly MpmDbContext _context;
    private readonly MaterialReceiptService _service;

    public MaterialReceiptServiceTests()
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MpmDbContext(options);
        _context.TenantId = "test-tenant";
        _service = new MaterialReceiptService(_context);
    }

    [Fact]
    public async Task CreateReceiptAsync_ValidData_CreatesReceiptAndInventoryLot()
    {
        // Arrange
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            TenantId = "test-tenant"
        };
        _context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S355",
            Dimension = "100x50x5",
            ProfileType = "RHS",
            Description = "Test Material",
            TenantId = "test-tenant"
        };
        _context.Materials.Add(material);

        var po = new PurchaseOrder
        {
            Number = "PO-001",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            TenantId = "test-tenant"
        };
        _context.PurchaseOrders.Add(po);

        var poLine = new PurchaseOrderLine
        {
            PurchaseOrderId = po.Id,
            MaterialId = material.Id,
            Quantity = 100,
            UnitPrice = 10.50m,
            TenantId = "test-tenant"
        };
        _context.PurchaseOrderLines.Add(poLine);

        await _context.SaveChangesAsync();

        var createDto = new CreateMaterialReceiptDto
        {
            PurchaseOrderId = po.Id,
            Number = "GRN-001",
            ReceivedBy = "John Doe",
            InvoiceNumber = "INV-001",
            PaymentTerms = "30 days",
            Lines = new List<CreateMaterialReceiptLineDto>
            {
                new CreateMaterialReceiptLineDto
                {
                    PurchaseOrderLineId = poLine.Id,
                    ReceivedQuantity = 95,
                    ActualUnitPrice = 10.50m,
                    CreateInventoryLot = true,
                    Location = "Warehouse A"
                }
            }
        };

        // Act
        var result = await _service.CreateReceiptAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("GRN-001", result.Number);
        Assert.Equal("John Doe", result.ReceivedBy);
        Assert.Equal("INV-001", result.InvoiceNumber);
        Assert.Equal("30 days", result.PaymentTerms);
        Assert.Single(result.Lines);
        Assert.Equal(95, result.Lines.First().ReceivedQuantity);
        Assert.Equal(-5, result.Lines.First().QuantityDeviation); // 95 - 100
        Assert.Single(result.Lines.First().InventoryLots);

        // Verify inventory lot was created
        var inventoryLot = await _context.InventoryLots.FirstOrDefaultAsync();
        Assert.NotNull(inventoryLot);
        Assert.Equal(95, inventoryLot.Quantity);
        Assert.Equal("Warehouse A", inventoryLot.Location);

        // Verify audit log was created
        var auditLog = await _context.InventoryAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("Created", auditLog.Action);
        Assert.Equal(95, auditLog.NewQuantity);
    }

    [Fact]
    public async Task CreateReceiptAsync_PartialDelivery_HandlesQuantityDeviation()
    {
        // Arrange
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            TenantId = "test-tenant"
        };
        _context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S355",
            Dimension = "100x50x5",
            ProfileType = "RHS",
            Description = "Test Material",
            TenantId = "test-tenant"
        };
        _context.Materials.Add(material);

        var po = new PurchaseOrder
        {
            Number = "PO-002",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            TenantId = "test-tenant"
        };
        _context.PurchaseOrders.Add(po);

        var poLine = new PurchaseOrderLine
        {
            PurchaseOrderId = po.Id,
            MaterialId = material.Id,
            Quantity = 100,
            UnitPrice = 10.50m,
            TenantId = "test-tenant"
        };
        _context.PurchaseOrderLines.Add(poLine);

        await _context.SaveChangesAsync();

        var createDto = new CreateMaterialReceiptDto
        {
            PurchaseOrderId = po.Id,
            Number = "GRN-002",
            ReceivedBy = "Jane Smith",
            IsPartialDelivery = true,
            Lines = new List<CreateMaterialReceiptLineDto>
            {
                new CreateMaterialReceiptLineDto
                {
                    PurchaseOrderLineId = poLine.Id,
                    ReceivedQuantity = 50, // Partial delivery
                    ActualUnitPrice = 11.00m, // Price adjustment
                    DeviationReason = "Partial delivery - rest to follow",
                    CreateInventoryLot = true,
                    Location = "Warehouse B"
                }
            }
        };

        // Act
        var result = await _service.CreateReceiptAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsPartialDelivery);
        Assert.Equal(50, result.Lines.First().ReceivedQuantity);
        Assert.Equal(-50, result.Lines.First().QuantityDeviation); // 50 - 100
        Assert.Equal(11.00m, result.Lines.First().ActualUnitPrice);
        Assert.Equal(10.50m, result.Lines.First().OriginalUnitPrice);
        Assert.Equal("Partial delivery - rest to follow", result.Lines.First().DeviationReason);
    }

    [Fact]
    public async Task CreateReceiptAsync_InvalidPurchaseOrder_ThrowsException()
    {
        // Arrange
        var createDto = new CreateMaterialReceiptDto
        {
            PurchaseOrderId = 999, // Non-existent PO
            Number = "GRN-003",
            ReceivedBy = "Test User",
            Lines = new List<CreateMaterialReceiptLineDto>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateReceiptAsync(createDto));
    }

    [Fact]
    public async Task GetReceiptByIdAsync_ValidId_ReturnsReceipt()
    {
        // Arrange
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            TenantId = "test-tenant"
        };
        _context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S355",
            Dimension = "100x50x5",
            ProfileType = "RHS",
            Description = "Test Material",
            TenantId = "test-tenant"
        };
        _context.Materials.Add(material);

        var po = new PurchaseOrder
        {
            Number = "PO-003",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            TenantId = "test-tenant"
        };
        _context.PurchaseOrders.Add(po);

        var grn = new GoodsReceiptNote
        {
            Number = "GRN-004",
            PurchaseOrderId = po.Id,
            ReceivedBy = "Test User",
            TenantId = "test-tenant"
        };
        _context.GoodsReceiptNotes.Add(grn);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetReceiptByIdAsync(grn.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("GRN-004", result.Number);
        Assert.Equal("Test User", result.ReceivedBy);
        Assert.Equal("PO-003", result.PurchaseOrderNumber);
    }

    [Fact]
    public async Task GetReceiptByIdAsync_InvalidId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetReceiptByIdAsync(999));
    }

    [Fact]
    public async Task DeleteReceiptAsync_ValidId_DeletesReceiptAndInventory()
    {
        // Arrange
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            TenantId = "test-tenant"
        };
        _context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S355",
            Dimension = "100x50x5",
            ProfileType = "RHS",
            Description = "Test Material",
            TenantId = "test-tenant"
        };
        _context.Materials.Add(material);

        var po = new PurchaseOrder
        {
            Number = "PO-005",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            TenantId = "test-tenant"
        };
        _context.PurchaseOrders.Add(po);

        var grn = new GoodsReceiptNote
        {
            Number = "GRN-005",
            PurchaseOrderId = po.Id,
            ReceivedBy = "Test User",
            TenantId = "test-tenant"
        };
        _context.GoodsReceiptNotes.Add(grn);

        var grnLine = new GoodsReceiptNoteLine
        {
            GoodsReceiptNoteId = grn.Id,
            PurchaseOrderLineId = 1,
            ReceivedQuantity = 100,
            TenantId = "test-tenant"
        };
        _context.GoodsReceiptNoteLines.Add(grnLine);

        var inventoryLot = new InventoryLot
        {
            MaterialId = material.Id,
            GoodsReceiptNoteLineId = grnLine.Id,
            Quantity = 100,
            IsReserved = false,
            TenantId = "test-tenant"
        };
        _context.InventoryLots.Add(inventoryLot);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteReceiptAsync(grn.Id);

        // Assert
        Assert.True(result);
        
        // Verify receipt was deleted
        var deletedGrn = await _context.GoodsReceiptNotes.FindAsync(grn.Id);
        Assert.Null(deletedGrn);
        
        // Verify inventory lot was deleted
        var deletedLot = await _context.InventoryLots.FindAsync(inventoryLot.Id);
        Assert.Null(deletedLot);
    }

    [Fact]
    public async Task DeleteReceiptAsync_ReservedInventory_ThrowsException()
    {
        // Arrange
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            TenantId = "test-tenant"
        };
        _context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S355",
            Dimension = "100x50x5",
            ProfileType = "RHS",
            Description = "Test Material",
            TenantId = "test-tenant"
        };
        _context.Materials.Add(material);

        var po = new PurchaseOrder
        {
            Number = "PO-006",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            TenantId = "test-tenant"
        };
        _context.PurchaseOrders.Add(po);

        var grn = new GoodsReceiptNote
        {
            Number = "GRN-006",
            PurchaseOrderId = po.Id,
            ReceivedBy = "Test User",
            TenantId = "test-tenant"
        };
        _context.GoodsReceiptNotes.Add(grn);

        var grnLine = new GoodsReceiptNoteLine
        {
            GoodsReceiptNoteId = grn.Id,
            PurchaseOrderLineId = 1,
            ReceivedQuantity = 100,
            TenantId = "test-tenant"
        };
        _context.GoodsReceiptNoteLines.Add(grnLine);

        var inventoryLot = new InventoryLot
        {
            MaterialId = material.Id,
            GoodsReceiptNoteLineId = grnLine.Id,
            Quantity = 100,
            IsReserved = true, // Reserved inventory
            TenantId = "test-tenant"
        };
        _context.InventoryLots.Add(inventoryLot);

        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.DeleteReceiptAsync(grn.Id));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}