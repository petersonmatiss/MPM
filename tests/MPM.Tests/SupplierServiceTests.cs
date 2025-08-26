using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services;

namespace MPM.Tests;

public class SupplierServiceTests
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
    public async Task CreateAsync_ValidSupplier_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new SupplierService(context);
        
        var supplier = new Supplier
        {
            Name = "Test Supplier Ltd",
            VatNumber = "LV12345678901",
            Email = "test@supplier.com",
            Currency = "EUR",
            IsActive = true
        };

        // Act
        var result = await service.CreateAsync(supplier);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Test Supplier Ltd", result.Name);
        Assert.Equal("LV12345678901", result.VatNumber);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSupplier_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new SupplierService(context);
        
        var supplier1 = new Supplier
        {
            Name = "Test Supplier Ltd",
            VatNumber = "LV12345678901",
            Email = "test@supplier.com",
            Currency = "EUR",
            IsActive = true
        };
        
        var supplier2 = new Supplier
        {
            Name = "Test Supplier Ltd",
            VatNumber = "LV12345678901",
            Email = "test2@supplier.com",
            Currency = "EUR",
            IsActive = true
        };

        // Act
        await service.CreateAsync(supplier1);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(supplier2));
    }

    [Fact]
    public async Task IsDuplicateAsync_ExistingSupplier_ShouldReturnTrue()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new SupplierService(context);
        
        var supplier = new Supplier
        {
            Name = "Test Supplier Ltd",
            VatNumber = "LV12345678901",
            Email = "test@supplier.com",
            Currency = "EUR",
            IsActive = true
        };
        
        await service.CreateAsync(supplier);

        // Act
        var isDuplicate = await service.IsDuplicateAsync("Test Supplier Ltd", "LV12345678901");

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicateAsync_NonExistingSupplier_ShouldReturnFalse()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new SupplierService(context);

        // Act
        var isDuplicate = await service.IsDuplicateAsync("Non-existing Supplier", "LV99999999999");

        // Assert
        Assert.False(isDuplicate);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveSuppliers()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new SupplierService(context);
        
        var supplier1 = new Supplier { Name = "Active Supplier", VatNumber = "LV111", IsActive = true };
        var supplier2 = new Supplier { Name = "Inactive Supplier", VatNumber = "LV222", IsActive = false };
        
        await service.CreateAsync(supplier1);
        context.Suppliers.Add(supplier2);
        await context.SaveChangesAsync();

        // Act
        var suppliers = await service.GetAllAsync();

        // Assert
        Assert.Single(suppliers);
        Assert.Equal("Active Supplier", suppliers.First().Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteSupplier()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new SupplierService(context);
        
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            VatNumber = "LV12345",
            IsActive = true
        };
        
        await service.CreateAsync(supplier);

        // Act
        await service.DeleteAsync(supplier.Id);

        // Assert
        var deletedSupplier = await context.Suppliers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == supplier.Id);
        Assert.NotNull(deletedSupplier);
        Assert.False(deletedSupplier.IsActive);
    }
}