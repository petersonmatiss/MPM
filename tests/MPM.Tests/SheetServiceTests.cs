using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services;
using Xunit;

namespace MPM.Tests;

public class SheetServiceTests
{
    private MpmDbContext GetTestDbContext()
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new MpmDbContext(options);
        context.TenantId = "test-tenant";
        return context;
    }

    [Fact]
    public async Task CreateAsync_ValidSheet_CreatesSheet()
    {
        // Arrange
        using var context = GetTestDbContext();
        var service = new SheetService(context);
        
        var sheet = new Sheet
        {
            SheetId = "TEST001",
            Grade = "S355",
            LengthMm = 6000,
            WidthMm = 3000,
            ThicknessMm = 10,
            Weight = 1413.0m
        };

        // Act
        var result = await service.CreateAsync(sheet);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST001", result.SheetId);
        Assert.Equal("S355", result.Grade);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSheetId_ThrowsException()
    {
        // Arrange
        using var context = GetTestDbContext();
        var service = new SheetService(context);
        
        var sheet1 = new Sheet
        {
            SheetId = "TEST001",
            Grade = "S355",
            LengthMm = 6000,
            WidthMm = 3000,
            ThicknessMm = 10,
            Weight = 1413.0m
        };
        
        var sheet2 = new Sheet
        {
            SheetId = "TEST001", // Same ID
            Grade = "S235",
            LengthMm = 4000,
            WidthMm = 2000,
            ThicknessMm = 8,
            Weight = 502.4m
        };

        // Act & Assert
        await service.CreateAsync(sheet1);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(sheet2));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CanDeleteAsync_SheetNotUsed_ReturnsTrue()
    {
        // Arrange
        using var context = GetTestDbContext();
        var service = new SheetService(context);
        
        var sheet = new Sheet
        {
            SheetId = "TEST001",
            Grade = "S355",
            LengthMm = 6000,
            WidthMm = 3000,
            ThicknessMm = 10,
            Weight = 1413.0m,
            IsUsed = false
        };
        
        await service.CreateAsync(sheet);

        // Act
        var canDelete = await service.CanDeleteAsync(sheet.Id);

        // Assert
        Assert.True(canDelete);
    }

    [Fact]
    public async Task CanDeleteAsync_SheetUsed_ReturnsFalse()
    {
        // Arrange
        using var context = GetTestDbContext();
        var service = new SheetService(context);
        
        var sheet = new Sheet
        {
            SheetId = "TEST001",
            Grade = "S355",
            LengthMm = 6000,
            WidthMm = 3000,
            ThicknessMm = 10,
            Weight = 1413.0m,
            IsUsed = true
        };
        
        await service.CreateAsync(sheet);

        // Act
        var canDelete = await service.CanDeleteAsync(sheet.Id);

        // Assert
        Assert.False(canDelete);
    }

    [Fact]
    public async Task GetAllAsync_WithThicknessFilter_ReturnsFilteredSheets()
    {
        // Arrange
        using var context = GetTestDbContext();
        var service = new SheetService(context);
        
        var sheet1 = new Sheet { SheetId = "S001", Grade = "S355", LengthMm = 6000, WidthMm = 3000, ThicknessMm = 10, Weight = 1413.0m };
        var sheet2 = new Sheet { SheetId = "S002", Grade = "S235", LengthMm = 4000, WidthMm = 2000, ThicknessMm = 8, Weight = 502.4m };
        var sheet3 = new Sheet { SheetId = "S003", Grade = "S355", LengthMm = 5000, WidthMm = 2500, ThicknessMm = 10, Weight = 982.5m };
        
        await service.CreateAsync(sheet1);
        await service.CreateAsync(sheet2);
        await service.CreateAsync(sheet3);

        // Act
        var result = await service.GetAllAsync(thicknessMm: 10);

        // Assert
        var sheets = result.ToList();
        Assert.Equal(2, sheets.Count);
        Assert.All(sheets, s => Assert.Equal(10, s.ThicknessMm));
    }
}