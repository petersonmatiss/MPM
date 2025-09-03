using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain;
using Mpm.Domain.Entities;
using Mpm.Services;
using Xunit;

namespace MPM.Tests;

public class PurchaseRequestServiceTests
{
    private MpmDbContext GetInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new MpmDbContext(options);
        context.TenantId = "test-tenant";
        return context;
    }

    private async Task<(PurchaseRequestService service, MpmDbContext context)> SetupServiceAsync()
    {
        var context = GetInMemoryContext();
        var auditService = new AuditService(context);
        var service = new PurchaseRequestService(context, auditService);

        // Add test data
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345678901",
            Currency = Constants.Currency.EUR,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        context.Suppliers.Add(supplier);

        var material = new Material
        {
            Grade = "S355",
            Dimension = "200x200x20",
            ProfileType = "Angle",
            UnitWeight = 1.5m,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        context.Materials.Add(material);

        await context.SaveChangesAsync();
        return (service, context);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetStatusToDraft()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var pr = new PurchaseRequest
        {
            Number = "PR-001",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };

        // Act
        var result = await service.CreateAsync(pr);

        // Assert
        Assert.Equal(PRStatus.Draft, result.Status);
        
        // Verify audit entry was created
        var auditEntries = await context.AuditEntries.ToListAsync();
        Assert.Single(auditEntries);
        Assert.Equal(AuditActions.Create, auditEntries[0].Action);
        Assert.Equal(AuditEntityTypes.PurchaseRequest, auditEntries[0].EntityType);
    }

    [Fact]
    public async Task SendForQuotesAsync_FromDraft_ShouldSucceed()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var material = await context.Materials.FirstAsync();
        
        var pr = new PurchaseRequest
        {
            Number = "PR-002",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);
        
        // Add a line item
        var line = new PurchaseRequestLine
        {
            MaterialId = material.Id,
            Quantity = 10,
            UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        await service.AddLineAsync(createdPr.Id, line, "user123", "test-user");

        // Act
        var result = await service.SendForQuotesAsync(createdPr.Id, "user123", "test-user", "Ready for quotes");

        // Assert
        Assert.Equal(PRStatus.Sent, result.Status);
        Assert.Equal("test-user", result.SentBy);
        Assert.NotNull(result.SentDate);
        
        // Verify audit entry
        var auditEntries = await context.AuditEntries
            .Where(a => a.Action == AuditActions.StatusChange)
            .ToListAsync();
        Assert.Single(auditEntries);
        Assert.Equal("Status", auditEntries[0].FieldName);
        Assert.Equal(PRStatus.Draft.ToString(), auditEntries[0].OldValue);
        Assert.Equal(PRStatus.Sent.ToString(), auditEntries[0].NewValue);
    }

    [Fact]
    public async Task SendForQuotesAsync_WithoutLines_ShouldThrowException()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var pr = new PurchaseRequest
        {
            Number = "PR-003",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendForQuotesAsync(createdPr.Id, "user123", "test-user"));
        
        Assert.Contains("Cannot send purchase request without line items", exception.Message);
    }

    [Fact]
    public async Task StartCollectingAsync_FromSent_ShouldSucceed()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var material = await context.Materials.FirstAsync();
        
        var pr = new PurchaseRequest
        {
            Number = "PR-004",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);
        
        var line = new PurchaseRequestLine
        {
            MaterialId = material.Id,
            Quantity = 10,
            UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        await service.AddLineAsync(createdPr.Id, line, "user123", "test-user");
        await service.SendForQuotesAsync(createdPr.Id, "user123", "test-user");

        // Act
        var result = await service.StartCollectingAsync(createdPr.Id, "user123", "test-user", "Started collecting quotes");

        // Assert
        Assert.Equal(PRStatus.Collecting, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_WithoutWinner_ShouldThrowException()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var material = await context.Materials.FirstAsync();
        
        var pr = new PurchaseRequest
        {
            Number = "PR-005",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);
        
        var line = new PurchaseRequestLine
        {
            MaterialId = material.Id,
            Quantity = 10,
            UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        await service.AddLineAsync(createdPr.Id, line, "user123", "test-user");
        await service.SendForQuotesAsync(createdPr.Id, "user123", "test-user");
        await service.StartCollectingAsync(createdPr.Id, "user123", "test-user");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CompleteAsync(createdPr.Id, "user123", "test-user"));
        
        Assert.Contains("Cannot complete purchase request without selecting a winner", exception.Message);
    }

    [Fact]
    public async Task SelectWinnerAsync_InCollectingStatus_ShouldSucceed()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var material = await context.Materials.FirstAsync();
        var supplier = await context.Suppliers.FirstAsync();
        
        var pr = new PurchaseRequest
        {
            Number = "PR-006",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);
        
        var line = new PurchaseRequestLine
        {
            MaterialId = material.Id,
            Quantity = 10,
            UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        await service.AddLineAsync(createdPr.Id, line, "user123", "test-user");
        await service.SendForQuotesAsync(createdPr.Id, "user123", "test-user");
        await service.StartCollectingAsync(createdPr.Id, "user123", "test-user");
        
        // Add a quote
        var quote = new PurchaseRequestQuote
        {
            SupplierId = supplier.Id,
            QuoteReference = "Q-001",
            Currency = Constants.Currency.EUR,
            TotalAmount = 1500m,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        await service.AddQuoteAsync(createdPr.Id, quote, "user123", "test-user");

        // Act
        var result = await service.SelectWinnerAsync(createdPr.Id, supplier.Id, "user123", "test-user", "Best price");

        // Assert
        Assert.Equal(supplier.Id, result.WinnerSupplierId);
        Assert.Equal("test-user", result.WinnerSelectedBy);
        Assert.NotNull(result.WinnerSelectedDate);
        Assert.Equal("Best price", result.WinnerSelectionReason);
        
        // Verify audit entry
        var auditEntries = await context.AuditEntries
            .Where(a => a.Action == AuditActions.WinnerSelection)
            .ToListAsync();
        Assert.Single(auditEntries);
    }

    [Theory]
    [InlineData(PRStatus.Draft, PRStatus.Completed)] // Invalid: Draft -> Completed
    [InlineData(PRStatus.Sent, PRStatus.Completed)]  // Invalid: Sent -> Completed  
    [InlineData(PRStatus.Completed, PRStatus.Sent)]  // Invalid: Completed -> Sent
    [InlineData(PRStatus.Completed, PRStatus.Collecting)] // Invalid: Completed -> Collecting
    public async Task InvalidStatusTransitions_ShouldThrowException(PRStatus fromStatus, PRStatus toStatus)
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        
        // This test verifies the ValidateStatusTransition method indirectly
        // by testing the specific transition methods that should fail
        
        var pr = new PurchaseRequest
        {
            Number = "PR-007",
            Title = "Test PR",
            RequestedBy = "test-user",
            Status = fromStatus, // Set initial status directly for test
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        context.PurchaseRequests.Add(pr);
        await context.SaveChangesAsync();

        // Act & Assert - Try invalid transitions
        InvalidOperationException? exception = null;
        
        try
        {
            if (toStatus == PRStatus.Sent)
                await service.SendForQuotesAsync(pr.Id, "user123", "test-user");
            else if (toStatus == PRStatus.Collecting)
                await service.StartCollectingAsync(pr.Id, "user123", "test-user");
            else if (toStatus == PRStatus.Completed)
                await service.CompleteAsync(pr.Id, "user123", "test-user");
            else if (toStatus == PRStatus.Canceled)
                await service.CancelAsync(pr.Id, "user123", "test-user", "Test cancellation");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }
        
        Assert.NotNull(exception);
        Assert.Contains("Invalid status transition", exception.Message);
    }

    [Fact]
    public async Task CancelAsync_WithoutReason_ShouldThrowException()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var pr = new PurchaseRequest
        {
            Number = "PR-008",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CancelAsync(createdPr.Id, "user123", "test-user", ""));
        
        Assert.Contains("Cancellation reason is required", exception.Message);
    }

    [Fact]
    public async Task AddLineAsync_ToNonDraftPR_ShouldThrowException()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var material = await context.Materials.FirstAsync();
        
        var pr = new PurchaseRequest
        {
            Number = "PR-009",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);
        
        // First add a line and send for quotes
        var firstLine = new PurchaseRequestLine
        {
            MaterialId = material.Id,
            Quantity = 5,
            UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        await service.AddLineAsync(createdPr.Id, firstLine, "user123", "test-user");
        await service.SendForQuotesAsync(createdPr.Id, "user123", "test-user");
        
        // Now try to add another line
        var secondLine = new PurchaseRequestLine
        {
            MaterialId = material.Id,
            Quantity = 10,
            UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddLineAsync(createdPr.Id, secondLine, "user123", "test-user"));
        
        Assert.Contains("Lines can only be added to draft purchase requests", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_NonDraftPR_ShouldThrowException()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var material = await context.Materials.FirstAsync();
        
        var pr = new PurchaseRequest
        {
            Number = "PR-010",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);
        
        var line = new PurchaseRequestLine
        {
            MaterialId = material.Id,
            Quantity = 10,
            UnitOfMeasure = Constants.UnitOfMeasure.Kilogram,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        await service.AddLineAsync(createdPr.Id, line, "user123", "test-user");
        await service.SendForQuotesAsync(createdPr.Id, "user123", "test-user");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAsync(createdPr.Id));
        
        Assert.Contains("Only draft purchase requests can be deleted", exception.Message);
    }

    [Fact]
    public async Task AuditService_GetEntityAuditTrail_ShouldReturnAuditEntries()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var pr = new PurchaseRequest
        {
            Number = "PR-011",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);
        var auditService = new AuditService(context);

        // Act
        var auditTrail = await auditService.GetEntityAuditTrailAsync(AuditEntityTypes.PurchaseRequest, createdPr.Id);

        // Assert
        Assert.Single(auditTrail);
        var auditEntry = auditTrail.First();
        Assert.Equal(AuditActions.Create, auditEntry.Action);
        Assert.Equal(AuditEntityTypes.PurchaseRequest, auditEntry.EntityType);
        Assert.Equal(createdPr.Id, auditEntry.EntityId);
        Assert.Equal("test-user", auditEntry.UserName);
    }

    [Fact]
    public async Task UpdateAsync_StatusChange_ShouldThrowException()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        var pr = new PurchaseRequest
        {
            Number = "PR-012",
            Title = "Test PR",
            RequestedBy = "test-user",
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var createdPr = await service.CreateAsync(pr);
        
        // Try to change status through regular update
        createdPr.Status = PRStatus.Sent;
        createdPr.UpdatedBy = "test-user";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(createdPr));
        
        Assert.Contains("Status changes must be performed through specific transition methods", exception.Message);
    }

    [Fact]
    public async Task TransitionsFromCompletedOrCanceled_ShouldThrowException()
    {
        // Arrange
        var (service, context) = await SetupServiceAsync();
        
        // Test that completed and canceled states don't allow any transitions
        var completedPr = new PurchaseRequest
        {
            Number = "PR-013",
            Title = "Completed PR",
            RequestedBy = "test-user",
            Status = PRStatus.Completed,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        var canceledPr = new PurchaseRequest
        {
            Number = "PR-014", 
            Title = "Canceled PR",
            RequestedBy = "test-user",
            Status = PRStatus.Canceled,
            TenantId = "test-tenant",
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
        
        context.PurchaseRequests.AddRange(completedPr, canceledPr);
        await context.SaveChangesAsync();

        // Act & Assert - Completed cannot transition to anything
        var exception1 = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelAsync(completedPr.Id, "user123", "test-user", "Test reason"));
        Assert.Contains("Invalid status transition", exception1.Message);

        // Act & Assert - Canceled cannot transition to anything  
        var exception2 = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendForQuotesAsync(canceledPr.Id, "user123", "test-user"));
        Assert.Contains("Invalid status transition", exception2.Message);
    }
}