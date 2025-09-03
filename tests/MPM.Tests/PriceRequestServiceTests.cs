using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain;
using Mpm.Domain.Entities;
using Mpm.Services;

namespace MPM.Tests;

public class PriceRequestServiceTests
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

    private async Task<SteelGrade> CreateTestSteelGrade(MpmDbContext context)
    {
        var steelGrade = new SteelGrade
        {
            Code = "S355",
            Name = "S355JR",
            Standard = "EN 10025",
            Description = "Non-alloy structural steel",
            DensityKgPerM3 = 7850,
            YieldStrengthMPa = 355,
            TensileStrengthMPa = 510,
            IsActive = true
        };
        
        context.SteelGrades.Add(steelGrade);
        await context.SaveChangesAsync();
        return steelGrade;
    }

    private async Task<Supplier> CreateTestSupplier(MpmDbContext context)
    {
        var supplier = new Supplier
        {
            Name = "Test Steel Supplier Ltd",
            VatNumber = "LV12345678901",
            Email = "test@supplier.com",
            Currency = "EUR",
            IsActive = true
        };
        
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    private async Task<ProfileType> CreateTestProfileType(MpmDbContext context)
    {
        var profileType = new ProfileType
        {
            Code = "HEB",
            Name = "HEB Beam",
            Category = "Beam",
            Description = "European wide flange beam",
            StandardWeight = 25.5m,
            DimensionFormat = "HxBxS",
            IsActive = true
        };
        
        context.ProfileTypes.Add(profileType);
        await context.SaveChangesAsync();
        return profileType;
    }

    [Fact]
    public async Task CreateAsync_ValidPriceRequestWithMixedLines_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        var profileType = await CreateTestProfileType(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request for mixed materials",
            RequestedBy = "Test Buyer",
            Notes = "Urgent request",
            Lines = new List<PriceRequestLine>
            {
                // Sheet line
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet for cutting",
                    LengthMm = 6000,
                    WidthMm = 2000,
                    ThicknessMm = 10,
                    SteelGradeId = steelGrade.Id,
                    Pieces = 5,
                    Notes = "Hot rolled sheet"
                },
                // Profile line
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Profile,
                    Description = "HEB 200 beams",
                    Dimension = "200x200x15",
                    TotalLength = 60000, // 60 meters total
                    Pieces = 5, // 5 pieces of 12m each
                    SteelGradeId = steelGrade.Id,
                    ProfileTypeId = profileType.Id,
                    Notes = "Standard length 12m"
                }
            }
        };

        // Act
        var result = await service.CreateAsync(priceRequest);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(PriceRequestStatus.Draft, result.Status);
        Assert.Equal("Test Buyer", result.RequestedBy);
        Assert.Equal(2, result.Lines.Count);
        
        // Verify sheet line
        var sheetLine = result.Lines.First(l => l.MaterialType == MaterialType.Sheet);
        Assert.Equal(6000, sheetLine.LengthMm);
        Assert.Equal(2000, sheetLine.WidthMm);
        Assert.Equal(10, sheetLine.ThicknessMm);
        Assert.Equal(steelGrade.Id, sheetLine.SteelGradeId);
        
        // Verify profile line
        var profileLine = result.Lines.First(l => l.MaterialType == MaterialType.Profile);
        Assert.Equal("200x200x15", profileLine.Dimension);
        Assert.Equal(60000, profileLine.TotalLength);
        Assert.Equal(5, profileLine.Pieces);
        Assert.Equal(steelGrade.Id, profileLine.SteelGradeId);
        Assert.Equal(profileType.Id, profileLine.ProfileTypeId);
        
        // Verify auto-generated number
        Assert.NotEmpty(result.Number);
        Assert.StartsWith("PR-", result.Number);
    }

    [Fact]
    public async Task CreateAsync_SheetLineWithoutRequiredDimensions_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    // Missing required dimensions
                    Pieces = 1
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(priceRequest));
    }

    [Fact]
    public async Task CreateAsync_ProfileLineWithoutRequiredFields_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Profile,
                    Description = "HEB beam",
                    // Missing Dimension, TotalLength, and Pieces
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(priceRequest));
    }

    [Fact]
    public async Task CreateAsync_ProfileLineWithoutProfileType_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Profile,
                    Description = "HEB beam",
                    Dimension = "200x200x15",
                    TotalLength = 12000,
                    Pieces = 1,
                    SteelGradeId = steelGrade.Id
                    // Missing ProfileTypeId
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(priceRequest));
        Assert.Contains("Profile type is required for profile materials", exception.Message);
    }

    [Fact]
    public async Task ChangeStatusAsync_ValidTransition_ShouldUpdateStatus()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);

        // Act
        var result = await service.ChangeStatusAsync(created.Id, PriceRequestStatus.Sent);

        // Assert
        Assert.Equal(PriceRequestStatus.Sent, result.Status);
        Assert.NotNull(result.SentDate);
    }

    [Fact]
    public async Task ChangeStatusAsync_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);

        // Act & Assert - Try to go directly from Draft to Completed (invalid)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangeStatusAsync(created.Id, PriceRequestStatus.Completed));
    }

    [Fact]
    public async Task AddSupplierAsync_ValidSupplier_ShouldAddSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        var supplier = await CreateTestSupplier(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);

        // Act
        var result = await service.AddSupplierAsync(created.Id, supplier.Id);

        // Assert
        Assert.Single(result.Suppliers);
        Assert.Equal(supplier.Id, result.Suppliers.First().SupplierId);
        Assert.NotNull(result.Suppliers.First().InvitedDate);
    }

    [Fact]
    public async Task AddSupplierAsync_DuplicateSupplier_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        var supplier = await CreateTestSupplier(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);
        await service.AddSupplierAsync(created.Id, supplier.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddSupplierAsync(created.Id, supplier.Id));
    }

    [Fact]
    public async Task DeleteAsync_DraftRequest_ShouldDeleteSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);

        // Act
        await service.DeleteAsync(created.Id);

        // Assert
        var result = await service.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_SentRequest_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);
        await service.ChangeStatusAsync(created.Id, PriceRequestStatus.Sent);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task AddLineAsync_DraftRequest_ShouldAddSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        var profileType = await CreateTestProfileType(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);

        var newLine = new PriceRequestLine
        {
            MaterialType = MaterialType.Profile,
            Description = "HEB beam",
            Dimension = "200x200x15",
            TotalLength = 12000,
            Pieces = 1,
            SteelGradeId = steelGrade.Id,
            ProfileTypeId = profileType.Id
        };

        // Act
        var result = await service.AddLineAsync(created.Id, newLine);

        // Assert
        Assert.Equal(2, result.Lines.Count);
        Assert.Contains(result.Lines, l => l.MaterialType == MaterialType.Profile);
    }

    [Fact]
    public async Task AddLineAsync_SentRequest_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test price request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);
        await service.ChangeStatusAsync(created.Id, PriceRequestStatus.Sent);

        var newLine = new PriceRequestLine
        {
            MaterialType = MaterialType.Profile,
            Description = "HEB beam",
            Dimension = "200x200x15",
            TotalLength = 12000,
            Pieces = 1,
            SteelGradeId = steelGrade.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddLineAsync(created.Id, newLine));
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnCorrectRequests()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        var steelGrade = await CreateTestSteelGrade(context);
        
        // Create multiple requests with different statuses
        var draftRequest = new PriceRequest
        {
            Description = "Draft request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var sentRequest = new PriceRequest
        {
            Description = "Sent request",
            RequestedBy = "Test Buyer",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Description = "Steel sheet",
                    LengthMm = 3000,
                    WidthMm = 1500,
                    ThicknessMm = 8,
                    Pieces = 1
                }
            }
        };

        var draft = await service.CreateAsync(draftRequest);
        var sent = await service.CreateAsync(sentRequest);
        await service.ChangeStatusAsync(sent.Id, PriceRequestStatus.Sent);

        // Act
        var draftResults = await service.GetByStatusAsync(PriceRequestStatus.Draft);
        var sentResults = await service.GetByStatusAsync(PriceRequestStatus.Sent);

        // Assert
        Assert.Single(draftResults);
        Assert.Single(sentResults);
        Assert.Equal(draft.Id, draftResults.First().Id);
        Assert.Equal(sent.Id, sentResults.First().Id);
    }
}