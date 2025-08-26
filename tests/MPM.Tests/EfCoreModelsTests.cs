using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;

namespace Mpm.Tests;

public class EfCoreModelsTests
{
    [Fact]
    public void DbContext_ShouldCreateWithInMemoryDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        // Act & Assert
        using var context = new MpmDbContext(options);
        Assert.NotNull(context);
        Assert.NotNull(context.Invoices);
        Assert.NotNull(context.Sheets);
        Assert.NotNull(context.Profiles);
        Assert.NotNull(context.ProfileRemnants);
        Assert.NotNull(context.SteelGrades);
        Assert.NotNull(context.ProfileTypes);
        Assert.NotNull(context.ManufacturingOrders);
        Assert.NotNull(context.SheetUsages);
        Assert.NotNull(context.ProfileUsages);
        Assert.NotNull(context.TimeLogs);
        Assert.NotNull(context.Notifications);
    }

    [Fact]
    public void NewEntities_ShouldHaveProperSoftDeleteAndConcurrencySupport()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb2")
            .Options;

        // Act & Assert
        using var context = new MpmDbContext(options);
        
        var invoice = new Invoice { Number = "INV-001" };
        var sheet = new Sheet { SheetId = "SH-001", LengthMm = 2000, WidthMm = 1000, ThicknessMm = 10 };
        var profile = new Profile { LotId = "A15", LengthMm = 6000, AvailableLengthMm = 6000 };
        
        // Verify all entities have soft delete and concurrency support
        Assert.False(invoice.IsDeleted);
        Assert.NotNull(invoice.RowVersion);
        Assert.False(sheet.IsDeleted);
        Assert.NotNull(sheet.RowVersion);
        Assert.False(profile.IsDeleted);
        Assert.NotNull(profile.RowVersion);
        
        // Verify integer mm lengths
        Assert.IsType<int>(sheet.LengthMm);
        Assert.IsType<int>(sheet.WidthMm);
        Assert.IsType<int>(sheet.ThicknessMm);
        Assert.IsType<int>(profile.LengthMm);
        Assert.IsType<int>(profile.AvailableLengthMm);
    }
}