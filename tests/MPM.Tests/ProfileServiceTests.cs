using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services;

namespace MPM.Tests;

public class ProfileServiceTests
{
    private static readonly string InMemoryDbName = "ProfileServiceTestsDb";
    private MpmDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: InMemoryDbName)
            .Options;

        var context = new MpmDbContext(options);
        context.TenantId = "test-tenant";
        return context;
    }

    [Fact]
    public async Task CreateAsync_ValidProfile_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            HeatNumber = "H123456",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        // Act
        var result = await service.CreateAsync(profile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("A15", result.LotId);
        Assert.Equal(12000, result.AvailableLengthMm); // Should be set to LengthMm initially
        
        var savedProfile = await context.Profiles.FirstAsync();
        Assert.Equal("A15", savedProfile.LotId);
    }

    [Fact]
    public async Task CreateAsync_InvalidLotIdPattern_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = "15A", // Invalid pattern - should be letter then number
            LengthMm = 12000,
            Weight = 1000.0m
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(profile));
        
        Assert.Contains("LotId must follow the pattern", exception.Message);
    }

    [Theory]
    [InlineData("A15")]
    [InlineData("B3")]
    [InlineData("Z999")]
    public async Task CreateAsync_ValidLotIdPatterns_ShouldSucceed(string lotId)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = lotId,
            LengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            HeatNumber = "H123456",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        // Act
        var result = await service.CreateAsync(profile);

        // Assert
        Assert.Equal(lotId, result.LotId);
    }

    [Theory]
    [InlineData("a15")] // lowercase letter
    [InlineData("15A")] // number first
    [InlineData("AB15")] // multiple letters
    [InlineData("A")] // no number
    [InlineData("15")] // no letter
    [InlineData("")] // empty
    public async Task CreateAsync_InvalidLotIdPatterns_ShouldThrowException(string lotId)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = lotId,
            LengthMm = 12000,
            Weight = 1000.0m
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(profile));
    }

    [Fact]
    public async Task CreateAsync_DuplicateLotId_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile1 = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        var profile2 = new Profile
        {
            LotId = "A15", // Same LotId
            LengthMm = 10000,
            Weight = 800.0m,
            Dimension = "180x180x12",
            SupplierName = "Another Supplier",
            UnitPrice = 45.00m
        };

        // Act
        await service.CreateAsync(profile1);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(profile2));
        
        Assert.Contains("A profile with LotId 'A15' already exists", exception.Message);
    }

    [Fact]
    public async Task GetByLotIdAsync_ExistingLotId_ShouldReturnProfile()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        await service.CreateAsync(profile);

        // Act
        var result = await service.GetByLotIdAsync("A15");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("A15", result.LotId);
        Assert.Equal(12000, result.LengthMm);
    }

    [Fact]
    public async Task GetByLotIdAsync_NonExistingLotId_ShouldReturnNull()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        // Act
        var result = await service.GetByLotIdAsync("A15");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CanDeleteAsync_ProfileWithUsage_ShouldReturnFalse()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        await service.CreateAsync(profile);

        // Add a usage record
        var usage = new ProfileUsage
        {
            ProfileId = profile.Id,
            UsedLengthMm = 2000,
            PiecesUsed = 1,
            UsageDate = DateTime.UtcNow,
            UsedBy = "Test User"
        };
        context.ProfileUsages.Add(usage);
        await context.SaveChangesAsync();

        // Act
        var canDelete = await service.CanDeleteAsync(profile.Id);

        // Assert
        Assert.False(canDelete);
    }

    [Fact]
    public async Task CanDeleteAsync_ProfileWithoutUsage_ShouldReturnTrue()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m,
            IsReserved = false
        };

        await service.CreateAsync(profile);

        // Act
        var canDelete = await service.CanDeleteAsync(profile.Id);

        // Assert
        Assert.True(canDelete);
    }

    [Fact]
    public async Task GetAvailableProfilesAsync_ShouldReturnOnlyUnreservedProfiles()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var availableProfile = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            AvailableLengthMm = 12000,
            Weight = 1000.0m,
            IsReserved = false,
            Dimension = "200x200x15",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        var reservedProfile = new Profile
        {
            LotId = "B3",
            LengthMm = 10000,
            AvailableLengthMm = 10000,
            Weight = 800.0m,
            IsReserved = true,
            Dimension = "180x180x12",
            SupplierName = "Test Supplier",
            UnitPrice = 45.00m
        };

        await service.CreateAsync(availableProfile);
        await service.CreateAsync(reservedProfile);

        // Act
        var availableProfiles = await service.GetAvailableProfilesAsync();

        // Assert
        Assert.Single(availableProfiles);
        Assert.Equal("A15", availableProfiles.First().LotId);
    }
}