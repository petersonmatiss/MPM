using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services;
using Mpm.Services.DTOs;

namespace MPM.Tests;

public class ProfileServiceTests
{
    private MpmDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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

    [Fact]
    public async Task UseProfileAsync_ValidUsage_ShouldDecrementStock()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            AvailableLengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        await service.CreateAsync(profile);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = 2000,
            PiecesUsed = 1,
            Notes = "Test usage"
        };

        // Act
        var usage = await service.UseProfileAsync("A15", request);

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(profile.Id, usage.ProfileId);
        Assert.Equal("Test User", usage.UsedBy);
        Assert.Equal(2000, usage.UsedLengthMm);
        Assert.Equal(1, usage.PiecesUsed);

        // Check that stock was decremented
        var updatedProfile = await service.GetByLotIdAsync("A15");
        Assert.NotNull(updatedProfile);
        Assert.Equal(10000, updatedProfile.AvailableLengthMm); // 12000 - (2000 * 1)
    }

    [Fact]
    public async Task UseProfileAsync_WithMultiplePieces_ShouldDecrementCorrectly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            AvailableLengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        await service.CreateAsync(profile);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = 1500,
            PiecesUsed = 3, // 3 pieces of 1500mm each = 4500mm total
            Notes = "Multiple pieces usage"
        };

        // Act
        var usage = await service.UseProfileAsync("A15", request);

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(1500, usage.UsedLengthMm);
        Assert.Equal(3, usage.PiecesUsed);

        // Check that stock was decremented correctly
        var updatedProfile = await service.GetByLotIdAsync("A15");
        Assert.NotNull(updatedProfile);
        Assert.Equal(7500, updatedProfile.AvailableLengthMm); // 12000 - (1500 * 3)
    }

    [Fact]
    public async Task UseProfileAsync_InsufficientStock_ShouldThrowException()
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

        // Create profile first which will set AvailableLengthMm correctly
        await service.CreateAsync(profile);
        
        // Now update it to have limited available length
        profile.AvailableLengthMm = 5000; // Only 5000mm available
        await service.UpdateAsync(profile);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = 3000,
            PiecesUsed = 2, // Need 6000mm total, but only 5000mm available
            Notes = "Too much usage"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UseProfileAsync("A15", request));

        Assert.Contains("Insufficient material available", exception.Message);
        Assert.Contains("Required: 6000mm, Available: 5000mm", exception.Message);

        // Check that stock was not changed
        var unchangedProfile = await service.GetByLotIdAsync("A15");
        Assert.NotNull(unchangedProfile);
        Assert.Equal(5000, unchangedProfile.AvailableLengthMm);
    }

    [Fact]
    public async Task UseProfileAsync_WithRemnant_ShouldCreateRemnantRecord()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var profile = new Profile
        {
            LotId = "A15",
            LengthMm = 12000,
            AvailableLengthMm = 12000,
            Weight = 1000.0m,
            Dimension = "200x200x15",
            SupplierName = "Test Supplier",
            UnitPrice = 50.00m
        };

        await service.CreateAsync(profile);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = 2000,
            PiecesUsed = 1,
            RemnantLengthMm = 500,
            Notes = "Usage with remnant"
        };

        // Act
        var usage = await service.UseProfileAsync("A15", request);

        // Assert
        Assert.NotNull(usage);
        Assert.True(usage.RemnantFlag);
        Assert.Equal(500, usage.RemnantLengthMm);

        // Check that remnant was created
        var remnants = await service.GetRemnantsAsync(profile.Id);
        Assert.Single(remnants);

        var remnant = remnants.First();
        Assert.Equal("A15-500", remnant.RemnantId);
        Assert.Equal(500, remnant.LengthMm);
        Assert.True(remnant.IsUsable);
        Assert.False(remnant.IsUsed);
        
        // Verify weight calculation
        var expectedWeight = profile.Weight * 500 / profile.LengthMm; // Proportional weight
        Assert.Equal(expectedWeight, remnant.Weight);
    }

    [Fact]
    public async Task UseProfileAsync_NonExistentProfile_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = 2000,
            PiecesUsed = 1,
            Notes = "Test usage"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UseProfileAsync("NONEXISTENT", request));

        Assert.Contains("Profile with LotId 'NONEXISTENT' not found", exception.Message);
    }

    [Theory]
    [InlineData("", "LotId is required")]
    [InlineData(null, "LotId is required")]
    public async Task UseProfileAsync_InvalidLotId_ShouldThrowArgumentException(string lotId, string expectedMessage)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = 2000,
            PiecesUsed = 1,
            Notes = "Test usage"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UseProfileAsync(lotId, request));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(0, "Used length must be greater than 0")]
    [InlineData(-100, "Used length must be greater than 0")]
    public async Task UseProfileAsync_InvalidUsedLength_ShouldThrowException(int usedLength, string expectedMessage)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = usedLength,
            PiecesUsed = 1,
            Notes = "Test usage"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UseProfileAsync("A15", request));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(0, "Pieces used must be greater than 0")]
    [InlineData(-1, "Pieces used must be greater than 0")]
    public async Task UseProfileAsync_InvalidPiecesUsed_ShouldThrowException(int piecesUsed, string expectedMessage)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = 2000,
            PiecesUsed = piecesUsed,
            Notes = "Test usage"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UseProfileAsync("A15", request));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task UseProfileAsync_EmptyUsedBy_ShouldThrowException(string usedBy)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProfileService(context);

        var request = new ProfileUsageRequest
        {
            UsedBy = usedBy,
            UsedLengthMm = 2000,
            PiecesUsed = 1,
            Notes = "Test usage"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UseProfileAsync("A15", request));

        Assert.Contains("UsedBy is required", exception.Message);
    }

    [Fact]
    public async Task UseProfileAsync_TransactionRollback_ShouldNotChangeData()
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

        // Create profile first which will set AvailableLengthMm correctly
        await service.CreateAsync(profile);
        
        // Now update it to have limited available length
        profile.AvailableLengthMm = 1000;
        await service.UpdateAsync(profile);

        var request = new ProfileUsageRequest
        {
            UsedBy = "Test User",
            UsedLengthMm = 2000, // This will fail - not enough stock
            PiecesUsed = 1,
            Notes = "This should fail"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UseProfileAsync("A15", request));

        // Verify no changes were made
        var unchangedProfile = await service.GetByLotIdAsync("A15");
        Assert.NotNull(unchangedProfile);
        Assert.Equal(1000, unchangedProfile.AvailableLengthMm);

        // Verify no usage records were created
        var usages = await context.ProfileUsages.Where(u => u.ProfileId == profile.Id).ToListAsync();
        Assert.Empty(usages);

        // Verify no remnants were created
        var remnants = await context.ProfileRemnants.Where(r => r.ProfileId == profile.Id).ToListAsync();
        Assert.Empty(remnants);
    }
}