using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Domain;
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

    [Fact]
    public async Task CreateAsync_ValidPriceRequest_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test Price Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft,
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Sheet,
                    Dimensions = "1000x2000x10",
                    SteelGrade = "S355",
                    PieceCount = 5
                }
            }
        };

        // Act
        var result = await service.CreateAsync(priceRequest);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.NotEmpty(result.Number);
        Assert.Equal("Test Price Request", result.Description);
        Assert.Equal(PriceRequestStatus.Draft, result.Status);
        Assert.Single(result.Lines);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNumber_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest1 = new PriceRequest
        {
            Number = "PR-TEST-001",
            Description = "First Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft
        };
        
        var priceRequest2 = new PriceRequest
        {
            Number = "PR-TEST-001",
            Description = "Second Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft
        };

        // Act
        await service.CreateAsync(priceRequest1);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(priceRequest2));
    }

    [Fact]
    public async Task GenerateNumberAsync_ShouldCreateUniqueSequentialNumber()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        // Create first price request to establish a sequence
        var firstRequest = new PriceRequest
        {
            Description = "First Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft
        };
        await service.CreateAsync(firstRequest);

        // Act
        var number1 = await service.GenerateNumberAsync();

        // Assert
        Assert.StartsWith("PR-", number1);
        Assert.Contains(DateTime.UtcNow.ToString("yyyy"), number1);
        Assert.Contains(DateTime.UtcNow.ToString("MM"), number1);
        
        // Should be different from the first request's number
        Assert.NotEqual(firstRequest.Number, number1);
    }

    [Fact]
    public async Task SubmitAsync_ValidDraftRequest_ShouldSubmitSuccessfully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Test Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft,
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Profile,
                    Dimensions = "200x200x10",
                    TotalLength = 6000,
                    SteelGrade = "S355",
                    ProfileType = "Square",
                    PieceCount = 2
                }
            }
        };

        var created = await service.CreateAsync(priceRequest);

        // Act
        var result = await service.SubmitAsync(created.Id);

        // Assert
        Assert.Equal(PriceRequestStatus.Submitted, result.Status);
        Assert.NotNull(result.SubmittedDate);
        Assert.True(result.SubmittedDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SubmitAsync_RequestWithoutLines_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Empty Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft,
            Lines = new List<PriceRequestLine>()
        };

        var created = await service.CreateAsync(priceRequest);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(created.Id));
    }

    [Fact]
    public async Task ValidateLineItems_ProfileWithoutProfileType_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Invalid Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft,
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Profile,
                    Dimensions = "200x200x10",
                    TotalLength = 6000,
                    SteelGrade = "S355",
                    ProfileType = "", // Empty profile type for profile material
                    PieceCount = 2
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(priceRequest));
    }

    [Fact]
    public async Task ValidateLineItems_ProfileWithZeroLength_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Description = "Invalid Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft,
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    MaterialType = MaterialType.Profile,
                    Dimensions = "200x200x10",
                    TotalLength = 0, // Invalid length
                    SteelGrade = "S355",
                    ProfileType = "Square",
                    PieceCount = 2
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(priceRequest));
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ShouldFilterCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var draftRequest = new PriceRequest
        {
            Description = "Draft Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft
        };
        
        var submittedRequest = new PriceRequest
        {
            Description = "Submitted Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Submitted,
            SubmittedDate = DateTime.UtcNow
        };

        await service.CreateAsync(draftRequest);
        await service.CreateAsync(submittedRequest);

        // Act
        var draftResults = await service.GetAllAsync(PriceRequestStatus.Draft);
        var submittedResults = await service.GetAllAsync(PriceRequestStatus.Submitted);

        // Assert
        Assert.Single(draftResults);
        Assert.Single(submittedResults);
        Assert.Equal(PriceRequestStatus.Draft, draftResults.First().Status);
        Assert.Equal(PriceRequestStatus.Submitted, submittedResults.First().Status);
    }

    [Fact]
    public async Task IsDuplicateAsync_ExistingNumber_ShouldReturnTrue()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);
        
        var priceRequest = new PriceRequest
        {
            Number = "PR-UNIQUE-001",
            Description = "Test Request",
            RequestedBy = "Test User",
            Status = PriceRequestStatus.Draft
        };
        
        await service.CreateAsync(priceRequest);

        // Act
        var isDuplicate = await service.IsDuplicateAsync("PR-UNIQUE-001");

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicateAsync_NonExistingNumber_ShouldReturnFalse()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new PriceRequestService(context);

        // Act
        var isDuplicate = await service.IsDuplicateAsync("PR-NONEXISTENT-001");

        // Assert
        Assert.False(isDuplicate);
    }
}