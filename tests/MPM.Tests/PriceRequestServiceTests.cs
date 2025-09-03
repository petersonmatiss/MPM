using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services;
using Xunit;

namespace MPM.Tests;

public class PriceRequestServiceTests : IDisposable
{
    private readonly MpmDbContext _context;
    private readonly PriceRequestService _service;
    private readonly TestPdfGenerationService _pdfService;
    private readonly TestEmailService _emailService;
    private readonly TestLogger<PriceRequestService> _logger;

    public PriceRequestServiceTests()
    {
        var options = new DbContextOptionsBuilder<MpmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MpmDbContext(options);
        _context.TenantId = "test-tenant";

        _pdfService = new TestPdfGenerationService();
        _emailService = new TestEmailService();
        _logger = new TestLogger<PriceRequestService>();

        _service = new PriceRequestService(_context, _pdfService, _emailService, _logger);
    }

    [Fact]
    public async Task CreateAsync_ShouldGeneratePriceRequestNumber()
    {
        // Arrange
        var priceRequest = new PriceRequest
        {
            Description = "Test Price Request",
            TenantId = "test-tenant"
        };

        // Act
        var result = await _service.CreateAsync(priceRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Number);
        Assert.StartsWith("PR2025", result.Number);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPriceRequest()
    {
        // Arrange
        var priceRequest = new PriceRequest
        {
            Number = "PR2025001",
            Description = "Test Price Request",
            TenantId = "test-tenant"
        };

        _context.PriceRequests.Add(priceRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(priceRequest.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PR2025001", result.Number);
        Assert.Equal("Test Price Request", result.Description);
    }

    [Fact]
    public async Task SendToSuppliersAsync_ShouldSendEmailsAndUpdateStatus()
    {
        // Arrange
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            Email = "test@supplier.com",
            VatNumber = "LV12345678901",
            TenantId = "test-tenant"
        };

        var material = new Material
        {
            Grade = "S355",
            Description = "Structural Steel",
            TenantId = "test-tenant"
        };

        var priceRequest = new PriceRequest
        {
            Number = "PR2025001",
            Description = "Test Price Request",
            Status = PriceRequestStatus.Draft,
            TenantId = "test-tenant",
            Lines = new List<PriceRequestLine>
            {
                new PriceRequestLine
                {
                    Material = material,
                    MaterialId = 1,
                    Quantity = 100,
                    UnitOfMeasure = "kg",
                    TenantId = "test-tenant"
                }
            }
        };

        _context.Suppliers.Add(supplier);
        _context.Materials.Add(material);
        _context.PriceRequests.Add(priceRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SendToSuppliersAsync(priceRequest.Id, new[] { supplier.Id });

        // Assert
        Assert.Equal(PriceRequestStatus.Sent, result.Status);
        
        var sendLogs = await _context.PriceRequestSends
            .Where(s => s.PriceRequestId == priceRequest.Id)
            .ToListAsync();

        Assert.Single(sendLogs);
        Assert.Equal(PriceRequestSendStatus.Sent, sendLogs[0].Status);
        Assert.Equal("test-hash", sendLogs[0].AttachmentHash);
        Assert.Equal(supplier.Email, sendLogs[0].RecipientEmail);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

// Test implementations
public class TestPdfGenerationService : IPdfGenerationService
{
    public Task<(byte[] PdfData, string Hash)> GeneratePriceRequestPdfAsync(PriceRequest priceRequest)
    {
        return Task.FromResult((new byte[] { 1, 2, 3 }, "test-hash"));
    }
}

public class TestEmailService : IEmailService
{
    public Task<EmailSendResult> SendEmailAsync(EmailMessage message)
    {
        return Task.FromResult(new EmailSendResult { Success = true, MessageId = "test-message-id" });
    }

    public bool IsValidEmailAddress(string email)
    {
        return !string.IsNullOrEmpty(email) && email.Contains("@");
    }
}

public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}