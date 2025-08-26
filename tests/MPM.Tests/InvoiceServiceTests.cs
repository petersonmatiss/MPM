using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Domain;
using Mpm.Services;

namespace MPM.Tests;

public class InvoiceServiceTests
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

    private async Task<Supplier> CreateTestSupplier(MpmDbContext context)
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
        await context.SaveChangesAsync();
        return supplier;
    }

    [Fact]
    public async Task CreateAsync_ValidInvoice_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var supplier = await CreateTestSupplier(context);
        var service = new InvoiceService(context);
        
        var invoice = new Invoice
        {
            Number = "INV-001",
            SupplierId = supplier.Id,
            InvoiceDate = DateTime.UtcNow,
            Currency = Constants.Currency.EUR,
            SubTotal = 100.00m,
            TaxAmount = 21.00m,
            TotalAmount = 121.00m
        };

        // Act
        var result = await service.CreateAsync(invoice);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("INV-001", result.Number);
        Assert.Equal(supplier.Id, result.SupplierId);
    }

    [Fact]
    public async Task CreateAsync_InvalidCurrency_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var supplier = await CreateTestSupplier(context);
        var service = new InvoiceService(context);
        
        var invoice = new Invoice
        {
            Number = "INV-002",
            SupplierId = supplier.Id,
            Currency = "XYZ" // Invalid currency
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(invoice));
        Assert.Contains("not a valid ISO 4217 currency code", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateInvoiceNumber_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var supplier = await CreateTestSupplier(context);
        var service = new InvoiceService(context);
        
        var invoice1 = new Invoice
        {
            Number = "INV-003",
            SupplierId = supplier.Id,
            Currency = Constants.Currency.EUR
        };
        
        var invoice2 = new Invoice
        {
            Number = "INV-003",
            SupplierId = supplier.Id,
            Currency = Constants.Currency.EUR
        };

        // Act
        await service.CreateAsync(invoice1);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(invoice2));
    }

    [Fact]
    public async Task GetAllAsync_WithSupplierFilter_ShouldReturnFilteredInvoices()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var supplier1 = await CreateTestSupplier(context);
        var supplier2 = new Supplier
        {
            Name = "Another Supplier",
            VatNumber = "LV98765432109",
            Currency = Constants.Currency.EUR,
            IsActive = true
        };
        context.Suppliers.Add(supplier2);
        await context.SaveChangesAsync();
        
        var invoice1 = new Invoice { Number = "INV-004", SupplierId = supplier1.Id, Currency = Constants.Currency.EUR };
        var invoice2 = new Invoice { Number = "INV-005", SupplierId = supplier2.Id, Currency = Constants.Currency.EUR };
        
        await service.CreateAsync(invoice1);
        await service.CreateAsync(invoice2);

        // Act
        var filteredInvoices = await service.GetAllAsync(supplierId: supplier1.Id);

        // Assert
        Assert.Single(filteredInvoices);
        Assert.Equal("INV-004", filteredInvoices.First().Number);
    }

    [Fact]
    public async Task GetAllAsync_WithDateFilter_ShouldReturnFilteredInvoices()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var supplier = await CreateTestSupplier(context);
        var service = new InvoiceService(context);
        
        var oldDate = DateTime.UtcNow.AddDays(-10);
        var recentDate = DateTime.UtcNow.AddDays(-1);
        
        var invoice1 = new Invoice { Number = "INV-006", SupplierId = supplier.Id, InvoiceDate = oldDate, Currency = Constants.Currency.EUR };
        var invoice2 = new Invoice { Number = "INV-007", SupplierId = supplier.Id, InvoiceDate = recentDate, Currency = Constants.Currency.EUR };
        
        await service.CreateAsync(invoice1);
        await service.CreateAsync(invoice2);

        // Act
        var filteredInvoices = await service.GetAllAsync(fromDate: DateTime.UtcNow.AddDays(-5));

        // Assert
        Assert.Single(filteredInvoices);
        Assert.Equal("INV-007", filteredInvoices.First().Number);
    }

    [Fact]
    public async Task CreateAsync_WithLines_ShouldCalculateTotals()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var supplier = await CreateTestSupplier(context);
        var service = new InvoiceService(context);
        
        var invoice = new Invoice
        {
            Number = "INV-008",
            SupplierId = supplier.Id,
            Currency = Constants.Currency.EUR,
            Lines = new List<InvoiceLine>
            {
                new InvoiceLine { Description = "Item 1", Quantity = 2, UnitPrice = 50.00m },
                new InvoiceLine { Description = "Item 2", Quantity = 1, UnitPrice = 30.00m }
            }
        };

        // Act
        var result = await service.CreateAsync(invoice);

        // Assert
        Assert.Equal(130.00m, result.SubTotal); // (2 * 50) + (1 * 30)
        Assert.Equal(100.00m, result.Lines.First().TotalPrice); // 2 * 50
        Assert.Equal(30.00m, result.Lines.Last().TotalPrice); // 1 * 30
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteInvoice()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var supplier = await CreateTestSupplier(context);
        var service = new InvoiceService(context);
        
        var invoice = new Invoice
        {
            Number = "INV-009",
            SupplierId = supplier.Id,
            Currency = Constants.Currency.EUR
        };
        
        await service.CreateAsync(invoice);

        // Act
        await service.DeleteAsync(invoice.Id);

        // Assert
        var deletedInvoice = await context.Invoices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        Assert.NotNull(deletedInvoice);
        Assert.True(deletedInvoice.IsDeleted);
    }

    [Fact]
    public async Task IsDuplicateAsync_ExistingInvoiceNumber_ShouldReturnTrue()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var supplier = await CreateTestSupplier(context);
        var service = new InvoiceService(context);
        
        var invoice = new Invoice
        {
            Number = "INV-010",
            SupplierId = supplier.Id,
            Currency = Constants.Currency.EUR
        };
        
        await service.CreateAsync(invoice);

        // Act
        var isDuplicate = await service.IsDuplicateAsync("INV-010");

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicateAsync_NonExistingInvoiceNumber_ShouldReturnFalse()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new InvoiceService(context);

        // Act
        var isDuplicate = await service.IsDuplicateAsync("INV-NONEXISTENT");

        // Assert
        Assert.False(isDuplicate);
    }
}