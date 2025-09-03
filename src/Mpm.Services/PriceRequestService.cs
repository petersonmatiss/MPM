using Mpm.Data;
using Mpm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mpm.Services;

public interface IPriceRequestService
{
    Task<IEnumerable<PriceRequest>> GetAllAsync();
    Task<PriceRequest?> GetByIdAsync(int id);
    Task<PriceRequest> CreateAsync(PriceRequest priceRequest);
    Task<PriceRequest> UpdateAsync(PriceRequest priceRequest);
    Task DeleteAsync(int id);
    Task<PriceRequest> SendToSuppliersAsync(int priceRequestId, int[] supplierIds);
    Task<PriceRequest> UpdateStatusAsync(int priceRequestId, PriceRequestStatus status);
    Task<IEnumerable<PriceRequestSend>> GetSendLogsAsync(int priceRequestId);
}

public class PriceRequestService : IPriceRequestService
{
    private readonly MpmDbContext _context;
    private readonly IPdfGenerationService _pdfService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PriceRequestService> _logger;

    public PriceRequestService(
        MpmDbContext context,
        IPdfGenerationService pdfService,
        IEmailService emailService,
        ILogger<PriceRequestService> logger)
    {
        _context = context;
        _pdfService = pdfService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<PriceRequest>> GetAllAsync()
    {
        return await _context.PriceRequests
            .Include(pr => pr.Project)
            .Include(pr => pr.Lines)
            .ThenInclude(l => l.Material)
            .OrderByDescending(pr => pr.RequestDate)
            .ToListAsync();
    }

    public async Task<PriceRequest?> GetByIdAsync(int id)
    {
        return await _context.PriceRequests
            .Include(pr => pr.Project)
            .Include(pr => pr.Lines)
            .ThenInclude(l => l.Material)
            .Include(pr => pr.Sends)
            .ThenInclude(s => s.Supplier)
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<PriceRequest> CreateAsync(PriceRequest priceRequest)
    {
        // Generate PR number if not provided
        if (string.IsNullOrEmpty(priceRequest.Number))
        {
            priceRequest.Number = await GeneratePriceRequestNumberAsync();
        }

        _context.PriceRequests.Add(priceRequest);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created price request {Number} (ID: {Id})", 
            priceRequest.Number, priceRequest.Id);
        
        return priceRequest;
    }

    public async Task<PriceRequest> UpdateAsync(PriceRequest priceRequest)
    {
        _context.PriceRequests.Update(priceRequest);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated price request {Number} (ID: {Id})", 
            priceRequest.Number, priceRequest.Id);
        
        return priceRequest;
    }

    public async Task DeleteAsync(int id)
    {
        var priceRequest = await _context.PriceRequests.FindAsync(id);
        if (priceRequest != null)
        {
            priceRequest.IsDeleted = true;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Soft deleted price request {Number} (ID: {Id})", 
                priceRequest.Number, priceRequest.Id);
        }
    }

    public async Task<PriceRequest> SendToSuppliersAsync(int priceRequestId, int[] supplierIds)
    {
        var priceRequest = await GetByIdAsync(priceRequestId);
        if (priceRequest == null)
            throw new InvalidOperationException($"Price request {priceRequestId} not found");

        var suppliers = await _context.Suppliers
            .Where(s => supplierIds.Contains(s.Id))
            .ToListAsync();

        if (suppliers.Count != supplierIds.Length)
            throw new InvalidOperationException("Some suppliers not found");

        // Generate PDF
        var (pdfData, hash) = await _pdfService.GeneratePriceRequestPdfAsync(priceRequest);

        // Send to each supplier
        foreach (var supplier in suppliers)
        {
            try
            {
                var emailMessage = new EmailMessage
                {
                    To = supplier.Email,
                    Subject = $"Price Request {priceRequest.Number}",
                    Body = GenerateEmailBody(priceRequest, supplier),
                    Attachments = new List<EmailAttachment>
                    {
                        new EmailAttachment
                        {
                            FileName = $"PR_{priceRequest.Number}.pdf",
                            Content = pdfData,
                            ContentType = "application/pdf"
                        }
                    }
                };

                var result = await _emailService.SendEmailAsync(emailMessage);

                var sendLog = new PriceRequestSend
                {
                    PriceRequestId = priceRequestId,
                    SupplierId = supplier.Id,
                    RecipientEmail = supplier.Email,
                    SentAt = DateTime.UtcNow,
                    Status = result.Success ? PriceRequestSendStatus.Sent : PriceRequestSendStatus.Failed,
                    AttachmentHash = hash,
                    EmailSubject = emailMessage.Subject,
                    EmailBody = emailMessage.Body,
                    ErrorMessage = result.ErrorMessage
                };

                _context.PriceRequestSends.Add(sendLog);

                _logger.LogInformation("Sent price request {Number} to supplier {SupplierName} ({Email}): {Status}",
                    priceRequest.Number, supplier.Name, supplier.Email, sendLog.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send price request {Number} to supplier {SupplierName} ({Email})",
                    priceRequest.Number, supplier.Name, supplier.Email);

                var sendLog = new PriceRequestSend
                {
                    PriceRequestId = priceRequestId,
                    SupplierId = supplier.Id,
                    RecipientEmail = supplier.Email,
                    SentAt = DateTime.UtcNow,
                    Status = PriceRequestSendStatus.Failed,
                    AttachmentHash = hash,
                    EmailSubject = $"Price Request {priceRequest.Number}",
                    EmailBody = GenerateEmailBody(priceRequest, supplier),
                    ErrorMessage = ex.Message
                };

                _context.PriceRequestSends.Add(sendLog);
            }
        }

        // Update PR status to Sent
        priceRequest.Status = PriceRequestStatus.Sent;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Price request {Number} sent to {Count} suppliers",
            priceRequest.Number, suppliers.Count);

        return priceRequest;
    }

    public async Task<PriceRequest> UpdateStatusAsync(int priceRequestId, PriceRequestStatus status)
    {
        var priceRequest = await _context.PriceRequests.FindAsync(priceRequestId);
        if (priceRequest == null)
            throw new InvalidOperationException($"Price request {priceRequestId} not found");

        priceRequest.Status = status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated price request {Number} status to {Status}",
            priceRequest.Number, status);

        return priceRequest;
    }

    public async Task<IEnumerable<PriceRequestSend>> GetSendLogsAsync(int priceRequestId)
    {
        return await _context.PriceRequestSends
            .Include(s => s.Supplier)
            .Where(s => s.PriceRequestId == priceRequestId)
            .OrderByDescending(s => s.SentAt)
            .ToListAsync();
    }

    private async Task<string> GeneratePriceRequestNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PR{year:0000}";
        
        var lastNumber = await _context.PriceRequests
            .Where(pr => pr.Number.StartsWith(prefix))
            .OrderByDescending(pr => pr.Number)
            .Select(pr => pr.Number)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastNumber) && lastNumber.Length > prefix.Length)
        {
            var numberPart = lastNumber.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int currentNumber))
            {
                nextNumber = currentNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:000}";
    }

    private static string GenerateEmailBody(PriceRequest priceRequest, Supplier supplier)
    {
        return $@"Dear {supplier.Name},

Please find attached our price request {priceRequest.Number}.

We kindly ask you to provide your quotation for the materials listed in the attached document.

{(priceRequest.RequiredDate.HasValue ? $"Required date: {priceRequest.RequiredDate:yyyy-MM-dd}" : "")}

{(!string.IsNullOrEmpty(priceRequest.Notes) ? $"Additional notes:\n{priceRequest.Notes}" : "")}

Please send your quotation to this email address.

Best regards,
MPM Team";
    }
}