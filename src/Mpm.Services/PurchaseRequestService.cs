using Mpm.Data;
using Mpm.Domain;
using Mpm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public interface IPurchaseRequestService
{
    Task<IEnumerable<PurchaseRequest>> GetAllAsync();
    Task<PurchaseRequest?> GetByIdAsync(int id);
    Task<PurchaseRequest> CreateAsync(PurchaseRequest purchaseRequest);
    Task<PurchaseRequest> UpdateAsync(PurchaseRequest purchaseRequest);
    Task DeleteAsync(int id);
    Task<IEnumerable<PurchaseRequest>> GetByProjectAsync(int projectId);
    Task<IEnumerable<PurchaseRequest>> GetByStatusAsync(PRStatus status);
    
    // Transition methods with validation
    Task<PurchaseRequest> SendForQuotesAsync(int id, string userId, string userName, string reason = "");
    Task<PurchaseRequest> StartCollectingAsync(int id, string userId, string userName, string reason = "");
    Task<PurchaseRequest> CompleteAsync(int id, string userId, string userName, string reason = "");
    Task<PurchaseRequest> CancelAsync(int id, string userId, string userName, string reason);
    Task<PurchaseRequest> SelectWinnerAsync(int id, int supplierId, string userId, string userName, string reason = "");
    
    // Line management
    Task<PurchaseRequestLine> AddLineAsync(int purchaseRequestId, PurchaseRequestLine line, string userId, string userName);
    Task<PurchaseRequestLine> UpdateLineAsync(PurchaseRequestLine line, string userId, string userName);
    Task RemoveLineAsync(int lineId, string userId, string userName);
    
    // Quote management
    Task<PurchaseRequestQuote> AddQuoteAsync(int purchaseRequestId, PurchaseRequestQuote quote, string userId, string userName);
    Task<PurchaseRequestQuote> UpdateQuoteAsync(PurchaseRequestQuote quote, string userId, string userName);
    Task RemoveQuoteAsync(int quoteId, string userId, string userName);
}

public interface IAuditService
{
    Task LogAsync(AuditEntry auditEntry);
    Task LogAsync(string entityType, int entityId, string action, string userId, string userName, 
                  string? fieldName = null, string? oldValue = null, string? newValue = null, 
                  string? reason = null, string? correlationId = null);
    Task<IEnumerable<AuditEntry>> GetEntityAuditTrailAsync(string entityType, int entityId);
    Task<IEnumerable<AuditEntry>> GetUserAuditTrailAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
}

public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly MpmDbContext _context;
    private readonly IAuditService _auditService;

    public PurchaseRequestService(MpmDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IEnumerable<PurchaseRequest>> GetAllAsync()
    {
        return await _context.PurchaseRequests
            .Include(pr => pr.Project)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.Material)
            .Include(pr => pr.Quotes)
                .ThenInclude(q => q.Supplier)
            .Include(pr => pr.WinnerSupplier)
            .ToListAsync();
    }

    public async Task<PurchaseRequest?> GetByIdAsync(int id)
    {
        return await _context.PurchaseRequests
            .Include(pr => pr.Project)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.Material)
            .Include(pr => pr.Quotes)
                .ThenInclude(q => q.Supplier)
            .Include(pr => pr.Quotes)
                .ThenInclude(q => q.Items)
            .Include(pr => pr.WinnerSupplier)
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<PurchaseRequest> CreateAsync(PurchaseRequest purchaseRequest)
    {
        purchaseRequest.Status = PRStatus.Draft;
        _context.PurchaseRequests.Add(purchaseRequest);
        await _context.SaveChangesAsync();
        
        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequest, purchaseRequest.Id, AuditActions.Create,
            purchaseRequest.CreatedBy, purchaseRequest.CreatedBy, reason: "Purchase request created");
        
        return purchaseRequest;
    }

    public async Task<PurchaseRequest> UpdateAsync(PurchaseRequest purchaseRequest)
    {
        var existingPR = await _context.PurchaseRequests.AsNoTracking().FirstOrDefaultAsync(pr => pr.Id == purchaseRequest.Id);
        if (existingPR == null)
            throw new InvalidOperationException($"Purchase request with ID {purchaseRequest.Id} not found");

        // Check if status change is being attempted through regular update
        if (existingPR.Status != purchaseRequest.Status)
        {
            throw new InvalidOperationException("Status changes must be performed through specific transition methods");
        }

        _context.PurchaseRequests.Update(purchaseRequest);
        await _context.SaveChangesAsync();
        
        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequest, purchaseRequest.Id, AuditActions.Update,
            purchaseRequest.UpdatedBy, purchaseRequest.UpdatedBy, reason: "Purchase request updated");
        
        return purchaseRequest;
    }

    public async Task DeleteAsync(int id)
    {
        var purchaseRequest = await _context.PurchaseRequests.FindAsync(id);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {id} not found");

        if (purchaseRequest.Status != PRStatus.Draft)
            throw new InvalidOperationException("Only draft purchase requests can be deleted");

        _context.PurchaseRequests.Remove(purchaseRequest);
        await _context.SaveChangesAsync();
        
        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequest, id, AuditActions.Delete,
            purchaseRequest.UpdatedBy, purchaseRequest.UpdatedBy, reason: "Purchase request deleted");
    }

    public async Task<IEnumerable<PurchaseRequest>> GetByProjectAsync(int projectId)
    {
        return await _context.PurchaseRequests
            .Where(pr => pr.ProjectId == projectId)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.Material)
            .Include(pr => pr.Quotes)
                .ThenInclude(q => q.Supplier)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseRequest>> GetByStatusAsync(PRStatus status)
    {
        return await _context.PurchaseRequests
            .Where(pr => pr.Status == status)
            .Include(pr => pr.Project)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.Material)
            .ToListAsync();
    }

    public async Task<PurchaseRequest> SendForQuotesAsync(int id, string userId, string userName, string reason = "")
    {
        var purchaseRequest = await GetByIdAsync(id);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {id} not found");

        ValidateStatusTransition(purchaseRequest.Status, PRStatus.Sent);

        // Validate that PR has lines
        if (!purchaseRequest.Lines.Any())
            throw new InvalidOperationException("Cannot send purchase request without line items");

        var oldStatus = purchaseRequest.Status;
        purchaseRequest.Status = PRStatus.Sent;
        purchaseRequest.SentBy = userName;
        purchaseRequest.SentDate = DateTime.UtcNow;
        purchaseRequest.UpdatedBy = userName;
        purchaseRequest.UpdatedAt = DateTime.UtcNow;

        _context.PurchaseRequests.Update(purchaseRequest);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequest, id, AuditActions.StatusChange,
            userId, userName, "Status", oldStatus.ToString(), PRStatus.Sent.ToString(), reason);

        return purchaseRequest;
    }

    public async Task<PurchaseRequest> StartCollectingAsync(int id, string userId, string userName, string reason = "")
    {
        var purchaseRequest = await GetByIdAsync(id);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {id} not found");

        ValidateStatusTransition(purchaseRequest.Status, PRStatus.Collecting);

        var oldStatus = purchaseRequest.Status;
        purchaseRequest.Status = PRStatus.Collecting;
        purchaseRequest.UpdatedBy = userName;
        purchaseRequest.UpdatedAt = DateTime.UtcNow;

        _context.PurchaseRequests.Update(purchaseRequest);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequest, id, AuditActions.StatusChange,
            userId, userName, "Status", oldStatus.ToString(), PRStatus.Collecting.ToString(), reason);

        return purchaseRequest;
    }

    public async Task<PurchaseRequest> CompleteAsync(int id, string userId, string userName, string reason = "")
    {
        var purchaseRequest = await GetByIdAsync(id);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {id} not found");

        ValidateStatusTransition(purchaseRequest.Status, PRStatus.Completed);

        // Validate that a winner has been selected
        if (purchaseRequest.WinnerSupplierId == null)
            throw new InvalidOperationException("Cannot complete purchase request without selecting a winner");

        var oldStatus = purchaseRequest.Status;
        purchaseRequest.Status = PRStatus.Completed;
        purchaseRequest.CompletedBy = userName;
        purchaseRequest.CompletedDate = DateTime.UtcNow;
        purchaseRequest.UpdatedBy = userName;
        purchaseRequest.UpdatedAt = DateTime.UtcNow;

        _context.PurchaseRequests.Update(purchaseRequest);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequest, id, AuditActions.StatusChange,
            userId, userName, "Status", oldStatus.ToString(), PRStatus.Completed.ToString(), reason);

        return purchaseRequest;
    }

    public async Task<PurchaseRequest> CancelAsync(int id, string userId, string userName, string reason)
    {
        var purchaseRequest = await GetByIdAsync(id);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {id} not found");

        ValidateStatusTransition(purchaseRequest.Status, PRStatus.Canceled);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required", nameof(reason));

        var oldStatus = purchaseRequest.Status;
        purchaseRequest.Status = PRStatus.Canceled;
        purchaseRequest.CanceledBy = userName;
        purchaseRequest.CanceledDate = DateTime.UtcNow;
        purchaseRequest.CancellationReason = reason;
        purchaseRequest.UpdatedBy = userName;
        purchaseRequest.UpdatedAt = DateTime.UtcNow;

        _context.PurchaseRequests.Update(purchaseRequest);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequest, id, AuditActions.StatusChange,
            userId, userName, "Status", oldStatus.ToString(), PRStatus.Canceled.ToString(), reason);

        return purchaseRequest;
    }

    public async Task<PurchaseRequest> SelectWinnerAsync(int id, int supplierId, string userId, string userName, string reason = "")
    {
        var purchaseRequest = await GetByIdAsync(id);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {id} not found");

        if (purchaseRequest.Status != PRStatus.Collecting)
            throw new InvalidOperationException("Winner can only be selected when PR is in Collecting status");

        // Validate that the supplier has submitted a quote
        var supplierQuote = purchaseRequest.Quotes.FirstOrDefault(q => q.SupplierId == supplierId);
        if (supplierQuote == null)
            throw new InvalidOperationException("Selected supplier has not submitted a quote for this PR");

        // Clear previous winner if any
        if (purchaseRequest.WinnerSupplierId.HasValue)
        {
            var previousQuote = purchaseRequest.Quotes.FirstOrDefault(q => q.SupplierId == purchaseRequest.WinnerSupplierId);
            if (previousQuote != null)
            {
                previousQuote.IsSelected = false;
                previousQuote.SelectedBy = "";
                previousQuote.SelectedDate = null;
                previousQuote.SelectionReason = "";
            }
        }

        // Set new winner
        purchaseRequest.WinnerSupplierId = supplierId;
        purchaseRequest.WinnerSelectedBy = userName;
        purchaseRequest.WinnerSelectedDate = DateTime.UtcNow;
        purchaseRequest.WinnerSelectionReason = reason;
        purchaseRequest.UpdatedBy = userName;
        purchaseRequest.UpdatedAt = DateTime.UtcNow;

        // Mark the selected quote
        supplierQuote.IsSelected = true;
        supplierQuote.SelectedBy = userName;
        supplierQuote.SelectedDate = DateTime.UtcNow;
        supplierQuote.SelectionReason = reason;

        _context.PurchaseRequests.Update(purchaseRequest);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequest, id, AuditActions.WinnerSelection,
            userId, userName, "WinnerSupplierId", 
            purchaseRequest.WinnerSupplierId?.ToString() ?? "", supplierId.ToString(), reason);

        return purchaseRequest;
    }

    public async Task<PurchaseRequestLine> AddLineAsync(int purchaseRequestId, PurchaseRequestLine line, string userId, string userName)
    {
        var purchaseRequest = await _context.PurchaseRequests.FindAsync(purchaseRequestId);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {purchaseRequestId} not found");

        if (purchaseRequest.Status != PRStatus.Draft)
            throw new InvalidOperationException("Lines can only be added to draft purchase requests");

        line.PurchaseRequestId = purchaseRequestId;
        line.CreatedBy = userName;
        line.UpdatedBy = userName;
        
        _context.PurchaseRequestLines.Add(line);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequestLine, line.Id, AuditActions.AddLine,
            userId, userName, reason: "Line added to purchase request");

        return line;
    }

    public async Task<PurchaseRequestLine> UpdateLineAsync(PurchaseRequestLine line, string userId, string userName)
    {
        var purchaseRequest = await _context.PurchaseRequests.FindAsync(line.PurchaseRequestId);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {line.PurchaseRequestId} not found");

        if (purchaseRequest.Status != PRStatus.Draft)
            throw new InvalidOperationException("Lines can only be updated on draft purchase requests");

        line.UpdatedBy = userName;
        line.UpdatedAt = DateTime.UtcNow;
        
        _context.PurchaseRequestLines.Update(line);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequestLine, line.Id, AuditActions.UpdateLine,
            userId, userName, reason: "Line updated");

        return line;
    }

    public async Task RemoveLineAsync(int lineId, string userId, string userName)
    {
        var line = await _context.PurchaseRequestLines.FindAsync(lineId);
        if (line == null)
            throw new InvalidOperationException($"Purchase request line with ID {lineId} not found");

        var purchaseRequest = await _context.PurchaseRequests.FindAsync(line.PurchaseRequestId);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {line.PurchaseRequestId} not found");

        if (purchaseRequest.Status != PRStatus.Draft)
            throw new InvalidOperationException("Lines can only be removed from draft purchase requests");

        _context.PurchaseRequestLines.Remove(line);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequestLine, lineId, AuditActions.RemoveLine,
            userId, userName, reason: "Line removed from purchase request");
    }

    public async Task<PurchaseRequestQuote> AddQuoteAsync(int purchaseRequestId, PurchaseRequestQuote quote, string userId, string userName)
    {
        var purchaseRequest = await _context.PurchaseRequests.FindAsync(purchaseRequestId);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {purchaseRequestId} not found");

        if (purchaseRequest.Status != PRStatus.Sent && purchaseRequest.Status != PRStatus.Collecting)
            throw new InvalidOperationException("Quotes can only be added to sent or collecting purchase requests");

        quote.PurchaseRequestId = purchaseRequestId;
        quote.CreatedBy = userName;
        quote.UpdatedBy = userName;
        
        _context.PurchaseRequestQuotes.Add(quote);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequestQuote, quote.Id, AuditActions.QuoteSubmission,
            userId, userName, reason: $"Quote submitted by {quote.Supplier?.Name ?? "Unknown"}");

        return quote;
    }

    public async Task<PurchaseRequestQuote> UpdateQuoteAsync(PurchaseRequestQuote quote, string userId, string userName)
    {
        var purchaseRequest = await _context.PurchaseRequests.FindAsync(quote.PurchaseRequestId);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {quote.PurchaseRequestId} not found");

        if (purchaseRequest.Status != PRStatus.Sent && purchaseRequest.Status != PRStatus.Collecting)
            throw new InvalidOperationException("Quotes can only be updated for sent or collecting purchase requests");

        quote.UpdatedBy = userName;
        quote.UpdatedAt = DateTime.UtcNow;
        
        _context.PurchaseRequestQuotes.Update(quote);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequestQuote, quote.Id, AuditActions.Update,
            userId, userName, reason: "Quote updated");

        return quote;
    }

    public async Task RemoveQuoteAsync(int quoteId, string userId, string userName)
    {
        var quote = await _context.PurchaseRequestQuotes.FindAsync(quoteId);
        if (quote == null)
            throw new InvalidOperationException($"Purchase request quote with ID {quoteId} not found");

        var purchaseRequest = await _context.PurchaseRequests.FindAsync(quote.PurchaseRequestId);
        if (purchaseRequest == null)
            throw new InvalidOperationException($"Purchase request with ID {quote.PurchaseRequestId} not found");

        if (purchaseRequest.Status != PRStatus.Sent && purchaseRequest.Status != PRStatus.Collecting)
            throw new InvalidOperationException("Quotes can only be removed from sent or collecting purchase requests");

        if (quote.IsSelected)
            throw new InvalidOperationException("Cannot remove a quote that has been selected as winner");

        _context.PurchaseRequestQuotes.Remove(quote);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditEntityTypes.PurchaseRequestQuote, quoteId, AuditActions.Delete,
            userId, userName, reason: "Quote removed");
    }

    private static void ValidateStatusTransition(PRStatus currentStatus, PRStatus newStatus)
    {
        var validTransitions = new Dictionary<PRStatus, List<PRStatus>>
        {
            [PRStatus.Draft] = new() { PRStatus.Sent, PRStatus.Canceled },
            [PRStatus.Sent] = new() { PRStatus.Collecting, PRStatus.Canceled },
            [PRStatus.Collecting] = new() { PRStatus.Completed, PRStatus.Canceled },
            [PRStatus.Completed] = new(), // No transitions from completed
            [PRStatus.Canceled] = new()   // No transitions from canceled
        };

        if (!validTransitions[currentStatus].Contains(newStatus))
        {
            throw new InvalidOperationException($"Invalid status transition from {currentStatus} to {newStatus}. " +
                $"Valid transitions from {currentStatus} are: {string.Join(", ", validTransitions[currentStatus])}");
        }
    }
}

public class AuditService : IAuditService
{
    private readonly MpmDbContext _context;

    public AuditService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(AuditEntry auditEntry)
    {
        _context.AuditEntries.Add(auditEntry);
        await _context.SaveChangesAsync();
    }

    public async Task LogAsync(string entityType, int entityId, string action, string userId, string userName,
        string? fieldName = null, string? oldValue = null, string? newValue = null,
        string? reason = null, string? correlationId = null)
    {
        var auditEntry = new AuditEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            FieldName = fieldName ?? "",
            OldValue = oldValue ?? "",
            NewValue = newValue ?? "",
            UserId = userId,
            UserName = userName,
            ActionDate = DateTime.UtcNow,
            Reason = reason ?? "",
            CorrelationId = correlationId ?? "",
            TenantId = _context.TenantId,
            CreatedBy = userName,
            UpdatedBy = userName
        };

        await LogAsync(auditEntry);
    }

    public async Task<IEnumerable<AuditEntry>> GetEntityAuditTrailAsync(string entityType, int entityId)
    {
        return await _context.AuditEntries
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.ActionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditEntry>> GetUserAuditTrailAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditEntries.Where(a => a.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(a => a.ActionDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.ActionDate <= toDate.Value);

        return await query
            .OrderByDescending(a => a.ActionDate)
            .ToListAsync();
    }
}