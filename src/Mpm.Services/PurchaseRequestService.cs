using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;

namespace Mpm.Services;

public interface IPurchaseRequestService
{
    Task<IEnumerable<PurchaseRequest>> GetAllAsync();
    Task<PurchaseRequest?> GetByIdAsync(int id);
    Task<PurchaseRequest> CreateAsync(PurchaseRequest purchaseRequest);
    Task<PurchaseRequest> UpdateAsync(PurchaseRequest purchaseRequest);
    Task DeleteAsync(int id);
    Task<PurchaseRequest> SetWinnerForLineAsync(int lineId, int? supplierId, int? quoteLineId);
    Task<PurchaseRequest> SetWinnerForAllLinesAsync(int purchaseRequestId, int supplierId);
    Task<bool> ValidateCompletionAsync(int purchaseRequestId);
    Task<PurchaseRequest> CompleteAsync(int purchaseRequestId);
    Task<Dictionary<int, decimal>> GetTotalsBySupplierAsync(int purchaseRequestId);
}

public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly MpmDbContext _context;

    public PurchaseRequestService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PurchaseRequest>> GetAllAsync()
    {
        return await _context.PurchaseRequests
            .Include(pr => pr.Project)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.Material)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.WinnerSupplier)
            .OrderByDescending(pr => pr.RequestDate)
            .ToListAsync();
    }

    public async Task<PurchaseRequest?> GetByIdAsync(int id)
    {
        return await _context.PurchaseRequests
            .Include(pr => pr.Project)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.Material)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.WinnerSupplier)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.WinnerQuoteLine!)
                    .ThenInclude(q => q.SupplierQuote)
                        .ThenInclude(sq => sq.Supplier)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.SupplierQuotes)
                    .ThenInclude(q => q.SupplierQuote)
                        .ThenInclude(sq => sq.Supplier)
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<PurchaseRequest> CreateAsync(PurchaseRequest purchaseRequest)
    {
        _context.PurchaseRequests.Add(purchaseRequest);
        await _context.SaveChangesAsync();
        return purchaseRequest;
    }

    public async Task<PurchaseRequest> UpdateAsync(PurchaseRequest purchaseRequest)
    {
        _context.PurchaseRequests.Update(purchaseRequest);
        await _context.SaveChangesAsync();
        return purchaseRequest;
    }

    public async Task DeleteAsync(int id)
    {
        var purchaseRequest = await _context.PurchaseRequests.FindAsync(id);
        if (purchaseRequest != null)
        {
            purchaseRequest.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PurchaseRequest> SetWinnerForLineAsync(int lineId, int? supplierId, int? quoteLineId)
    {
        var line = await _context.PurchaseRequestLines
            .Include(l => l.PurchaseRequest)
            .FirstOrDefaultAsync(l => l.Id == lineId);

        if (line == null)
            throw new ArgumentException($"Purchase request line with ID {lineId} not found");

        // Validate that the supplier and quote line are compatible
        if (supplierId.HasValue && quoteLineId.HasValue)
        {
            var quoteLine = await _context.SupplierQuoteLines
                .Include(q => q.SupplierQuote)
                .FirstOrDefaultAsync(q => q.Id == quoteLineId.Value);

            if (quoteLine == null || quoteLine.SupplierQuote.SupplierId != supplierId.Value)
                throw new ArgumentException("The selected quote line does not belong to the selected supplier");

            if (quoteLine.PurchaseRequestLineId != lineId)
                throw new ArgumentException("The selected quote line does not belong to this purchase request line");
        }

        line.WinnerSupplierId = supplierId;
        line.WinnerQuoteLineId = quoteLineId;
        line.WinnerSelectedDate = supplierId.HasValue ? DateTime.UtcNow : null;
        line.WinnerSelectedBy = supplierId.HasValue ? "System" : string.Empty; // TODO: Get from current user

        await _context.SaveChangesAsync();

        return await GetByIdAsync(line.PurchaseRequestId) ?? line.PurchaseRequest;
    }

    public async Task<PurchaseRequest> SetWinnerForAllLinesAsync(int purchaseRequestId, int supplierId)
    {
        var purchaseRequest = await _context.PurchaseRequests
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.SupplierQuotes)
                    .ThenInclude(q => q.SupplierQuote)
            .FirstOrDefaultAsync(pr => pr.Id == purchaseRequestId);

        if (purchaseRequest == null)
            throw new ArgumentException($"Purchase request with ID {purchaseRequestId} not found");

        // Validate that the supplier has quotes for all lines
        var supplierQuoteLines = await _context.SupplierQuoteLines
            .Include(q => q.SupplierQuote)
            .Where(q => q.SupplierQuote.SupplierId == supplierId && 
                       q.SupplierQuote.PurchaseRequestId == purchaseRequestId)
            .ToListAsync();

        var linesWithQuotes = supplierQuoteLines.Select(q => q.PurchaseRequestLineId).Distinct().ToList();
        var allLineIds = purchaseRequest.Lines.Select(l => l.Id).ToList();

        var missingQuotes = allLineIds.Except(linesWithQuotes).ToList();
        if (missingQuotes.Any())
        {
            throw new InvalidOperationException(
                $"The selected supplier does not have quotes for all lines. Missing quotes for line IDs: {string.Join(", ", missingQuotes)}");
        }

        // Set winner for each line
        foreach (var line in purchaseRequest.Lines)
        {
            var bestQuote = supplierQuoteLines
                .Where(q => q.PurchaseRequestLineId == line.Id)
                .OrderBy(q => q.TotalPrice)
                .FirstOrDefault();

            if (bestQuote != null)
            {
                line.WinnerSupplierId = supplierId;
                line.WinnerQuoteLineId = bestQuote.Id;
                line.WinnerSelectedDate = DateTime.UtcNow;
                line.WinnerSelectedBy = "System"; // TODO: Get from current user
            }
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(purchaseRequestId) ?? purchaseRequest;
    }

    public async Task<bool> ValidateCompletionAsync(int purchaseRequestId)
    {
        var linesWithoutWinner = await _context.PurchaseRequestLines
            .Where(l => l.PurchaseRequestId == purchaseRequestId && l.WinnerSupplierId == null)
            .CountAsync();

        return linesWithoutWinner == 0;
    }

    public async Task<PurchaseRequest> CompleteAsync(int purchaseRequestId)
    {
        var purchaseRequest = await _context.PurchaseRequests.FindAsync(purchaseRequestId);
        if (purchaseRequest == null)
            throw new ArgumentException($"Purchase request with ID {purchaseRequestId} not found");

        if (!await ValidateCompletionAsync(purchaseRequestId))
            throw new InvalidOperationException("Cannot complete purchase request: some lines do not have a winner selected");

        purchaseRequest.IsCompleted = true;
        purchaseRequest.CompletedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(purchaseRequestId) ?? purchaseRequest;
    }

    public async Task<Dictionary<int, decimal>> GetTotalsBySupplierAsync(int purchaseRequestId)
    {
        var totals = await _context.PurchaseRequestLines
            .Where(l => l.PurchaseRequestId == purchaseRequestId && l.WinnerSupplierId != null)
            .Include(l => l.WinnerQuoteLine)
            .GroupBy(l => l.WinnerSupplierId)
            .Select(g => new
            {
                SupplierId = g.Key!.Value,
                Total = g.Sum(l => l.WinnerQuoteLine!.TotalPrice)
            })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Total);

        return totals;
    }
}