using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain;
using Mpm.Domain.Entities;
using Mpm.Services;

namespace MPM.Tests;

public class PurchaseOrderServiceTests
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
    public async Task CreateAsync_ShouldCreatePurchaseOrderWithDraftStatus()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PurchaseOrderService(context);

        var supplier = new Supplier { Name = "Test Supplier", VatNumber = "LV12345678901" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-001",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow
        };

        // Act
        var result = await service.CreateAsync(purchaseOrder);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.Draft, result.Status);
        Assert.Equal("PO-001", result.Number);
        Assert.False(result.IsConfirmed);
    }

    [Fact]
    public async Task SendToSupplierAsync_ShouldUpdateStatusAndSentDate()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PurchaseOrderService(context);

        var supplier = new Supplier { Name = "Test Supplier", VatNumber = "LV12345678901" };
        context.Suppliers.Add(supplier);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-002",
            SupplierId = supplier.Id,
            Status = PurchaseOrderStatus.Draft,
            IsConfirmed = false
        };
        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SendToSupplierAsync(purchaseOrder.Id);

        // Assert
        Assert.Equal(PurchaseOrderStatus.Sent, result.Status);
        Assert.NotNull(result.SentDate);
        Assert.True(result.IsConfirmed);
        Assert.True(result.SentDate.Value.Date == DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateStatusAndConfirmedFlag()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PurchaseOrderService(context);

        var supplier = new Supplier { Name = "Test Supplier", VatNumber = "LV12345678901" };
        context.Suppliers.Add(supplier);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-003",
            SupplierId = supplier.Id,
            Status = PurchaseOrderStatus.Sent,
            IsConfirmed = true
        };
        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateStatusAsync(purchaseOrder.Id, PurchaseOrderStatus.Acknowledged);

        // Assert
        Assert.Equal(PurchaseOrderStatus.Acknowledged, result.Status);
        Assert.True(result.IsConfirmed); // Should remain true for status >= Sent
    }

    [Fact]
    public async Task AddDocumentAsync_ShouldAddDocumentToPurchaseOrder()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PurchaseOrderService(context);

        var supplier = new Supplier { Name = "Test Supplier", VatNumber = "LV12345678901" };
        context.Suppliers.Add(supplier);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-004",
            SupplierId = supplier.Id
        };
        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        var document = new PurchaseOrderDocument
        {
            PurchaseOrderId = purchaseOrder.Id,
            FileName = "contract.pdf",
            OriginalFileName = "contract.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            DocumentType = "Contract",
            Description = "Purchase contract"
        };

        // Act
        var result = await service.AddDocumentAsync(document);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("contract.pdf", result.FileName);
        Assert.Equal("Contract", result.DocumentType);
        Assert.Equal(purchaseOrder.Id, result.PurchaseOrderId);
    }

    [Fact]
    public async Task AddCommunicationAsync_ShouldAddCommunicationToPurchaseOrder()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PurchaseOrderService(context);

        var supplier = new Supplier { Name = "Test Supplier", VatNumber = "LV12345678901" };
        context.Suppliers.Add(supplier);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-005",
            SupplierId = supplier.Id
        };
        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        var communication = new PurchaseOrderCommunication
        {
            PurchaseOrderId = purchaseOrder.Id,
            Subject = "Order Confirmation",
            Content = "Please confirm receipt of order",
            CommunicationType = "Email",
            Direction = "Outbound",
            ContactPerson = "John Doe",
            ContactEmail = "john@supplier.com"
        };

        // Act
        var result = await service.AddCommunicationAsync(communication);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Order Confirmation", result.Subject);
        Assert.Equal("Email", result.CommunicationType);
        Assert.Equal("Outbound", result.Direction);
        Assert.Equal(purchaseOrder.Id, result.PurchaseOrderId);
    }

    [Fact]
    public async Task GetDocumentsAsync_ShouldReturnDocumentsForPurchaseOrder()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PurchaseOrderService(context);

        var supplier = new Supplier { Name = "Test Supplier", VatNumber = "LV12345678901" };
        context.Suppliers.Add(supplier);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-006",
            SupplierId = supplier.Id
        };
        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        var documents = new[]
        {
            new PurchaseOrderDocument { PurchaseOrderId = purchaseOrder.Id, FileName = "doc1.pdf" },
            new PurchaseOrderDocument { PurchaseOrderId = purchaseOrder.Id, FileName = "doc2.pdf" }
        };
        context.PurchaseOrderDocuments.AddRange(documents);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDocumentsAsync(purchaseOrder.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.FileName == "doc1.pdf");
        Assert.Contains(result, d => d.FileName == "doc2.pdf");
    }

    [Fact]
    public async Task GetCommunicationsAsync_ShouldReturnCommunicationsForPurchaseOrder()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PurchaseOrderService(context);

        var supplier = new Supplier { Name = "Test Supplier", VatNumber = "LV12345678901" };
        context.Suppliers.Add(supplier);

        var purchaseOrder = new PurchaseOrder
        {
            Number = "PO-007",
            SupplierId = supplier.Id
        };
        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        var communications = new[]
        {
            new PurchaseOrderCommunication { PurchaseOrderId = purchaseOrder.Id, Subject = "Initial inquiry" },
            new PurchaseOrderCommunication { PurchaseOrderId = purchaseOrder.Id, Subject = "Follow up" }
        };
        context.PurchaseOrderCommunications.AddRange(communications);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCommunicationsAsync(purchaseOrder.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, c => c.Subject == "Initial inquiry");
        Assert.Contains(result, c => c.Subject == "Follow up");
    }
}