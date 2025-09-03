using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;

namespace Mpm.Services;

public interface ISupplierQuoteService
{
    Task<IEnumerable<SupplierQuote>> GetAllAsync();
    Task<SupplierQuote?> GetByIdAsync(int id);
    Task<IEnumerable<SupplierQuote>> GetByPurchaseRequestAsync(int purchaseRequestId);
    Task<IEnumerable<SupplierQuote>> GetBySupplierAsync(int supplierId);
    Task<SupplierQuote> CreateAsync(SupplierQuote supplierQuote);
    Task<SupplierQuote> UpdateAsync(SupplierQuote supplierQuote);
    Task DeleteAsync(int id);
    Task<SupplierQuote> CalculateTotalsAsync(int supplierQuoteId);
    Task<bool> HasQuoteForAllLinesAsync(int purchaseRequestId, int supplierId);
}

public class SupplierQuoteService : ISupplierQuoteService
{
    private readonly MpmDbContext _context;

    public SupplierQuoteService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SupplierQuote>> GetAllAsync()
    {
        return await _context.SupplierQuotes
            .Include(sq => sq.PurchaseRequest)
            .Include(sq => sq.Supplier)
            .Include(sq => sq.Lines)
                .ThenInclude(l => l.PurchaseRequestLine)
                    .ThenInclude(prl => prl.Material)
            .OrderByDescending(sq => sq.QuoteDate)
            .ToListAsync();
    }

    public async Task<SupplierQuote?> GetByIdAsync(int id)
    {
        return await _context.SupplierQuotes
            .Include(sq => sq.PurchaseRequest)
            .Include(sq => sq.Supplier)
            .Include(sq => sq.Lines)
                .ThenInclude(l => l.PurchaseRequestLine)
                    .ThenInclude(prl => prl.Material)
            .FirstOrDefaultAsync(sq => sq.Id == id);
    }

    public async Task<IEnumerable<SupplierQuote>> GetByPurchaseRequestAsync(int purchaseRequestId)
    {
        return await _context.SupplierQuotes
            .Include(sq => sq.Supplier)
            .Include(sq => sq.Lines)
                .ThenInclude(l => l.PurchaseRequestLine)
                    .ThenInclude(prl => prl.Material)
            .Where(sq => sq.PurchaseRequestId == purchaseRequestId && sq.IsActive)
            .OrderBy(sq => sq.Supplier.Name)
            .ThenByDescending(sq => sq.QuoteDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<SupplierQuote>> GetBySupplierAsync(int supplierId)
    {
        return await _context.SupplierQuotes
            .Include(sq => sq.PurchaseRequest)
            .Include(sq => sq.Lines)
                .ThenInclude(l => l.PurchaseRequestLine)
                    .ThenInclude(prl => prl.Material)
            .Where(sq => sq.SupplierId == supplierId && sq.IsActive)
            .OrderByDescending(sq => sq.QuoteDate)
            .ToListAsync();
    }

    public async Task<SupplierQuote> CreateAsync(SupplierQuote supplierQuote)
    {
        // Validate that the purchase request exists
        var purchaseRequest = await _context.PurchaseRequests
            .Include(pr => pr.Lines)
            .FirstOrDefaultAsync(pr => pr.Id == supplierQuote.PurchaseRequestId);

        if (purchaseRequest == null)
            throw new ArgumentException($"Purchase request with ID {supplierQuote.PurchaseRequestId} not found");

        // Check for duplicate quote from same supplier
        var existingQuote = await _context.SupplierQuotes
            .FirstOrDefaultAsync(sq => sq.PurchaseRequestId == supplierQuote.PurchaseRequestId && 
                                      sq.SupplierId == supplierQuote.SupplierId && 
                                      sq.IsActive);

        if (existingQuote != null)
        {
            throw new InvalidOperationException(
                $"An active quote from this supplier already exists for this purchase request");
        }

        // Calculate totals for each line
        foreach (var line in supplierQuote.Lines)
        {
            line.TotalPrice = line.UnitPrice * 
                             purchaseRequest.Lines.First(l => l.Id == line.PurchaseRequestLineId).Quantity;
            
            if (line.DiscountPercent > 0)
            {
                line.TotalPrice -= line.TotalPrice * (line.DiscountPercent / 100);
            }
        }

        _context.SupplierQuotes.Add(supplierQuote);
        await _context.SaveChangesAsync();

        // Calculate and update total amount
        await CalculateTotalsAsync(supplierQuote.Id);

        return await GetByIdAsync(supplierQuote.Id) ?? supplierQuote;
    }

    public async Task<SupplierQuote> UpdateAsync(SupplierQuote supplierQuote)
    {
        var existingQuote = await _context.SupplierQuotes
            .Include(sq => sq.Lines)
            .FirstOrDefaultAsync(sq => sq.Id == supplierQuote.Id);

        if (existingQuote == null)
            throw new ArgumentException($"Supplier quote with ID {supplierQuote.Id} not found");

        // Update quote properties
        existingQuote.QuoteNumber = supplierQuote.QuoteNumber;
        existingQuote.QuoteDate = supplierQuote.QuoteDate;
        existingQuote.ValidUntil = supplierQuote.ValidUntil;
        existingQuote.Currency = supplierQuote.Currency;
        existingQuote.PaymentTerms = supplierQuote.PaymentTerms;
        existingQuote.DeliveryTerms = supplierQuote.DeliveryTerms;
        existingQuote.LeadTimeDays = supplierQuote.LeadTimeDays;
        existingQuote.Notes = supplierQuote.Notes;
        existingQuote.IsActive = supplierQuote.IsActive;

        // Update lines
        var purchaseRequest = await _context.PurchaseRequests
            .Include(pr => pr.Lines)
            .FirstOrDefaultAsync(pr => pr.Id == existingQuote.PurchaseRequestId);

        if (purchaseRequest != null)
        {
            foreach (var line in existingQuote.Lines)
            {
                var updatedLine = supplierQuote.Lines.FirstOrDefault(l => l.Id == line.Id);
                if (updatedLine != null)
                {
                    line.UnitPrice = updatedLine.UnitPrice;
                    line.DiscountPercent = updatedLine.DiscountPercent;
                    line.LeadTimeDays = updatedLine.LeadTimeDays;
                    line.Notes = updatedLine.Notes;
                    line.IsAvailable = updatedLine.IsAvailable;

                    // Recalculate total price
                    var quantity = purchaseRequest.Lines.First(l => l.Id == line.PurchaseRequestLineId).Quantity;
                    line.TotalPrice = line.UnitPrice * quantity;
                    
                    if (line.DiscountPercent > 0)
                    {
                        line.TotalPrice -= line.TotalPrice * (line.DiscountPercent / 100);
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        // Recalculate and update total amount
        await CalculateTotalsAsync(existingQuote.Id);

        return await GetByIdAsync(existingQuote.Id) ?? existingQuote;
    }

    public async Task DeleteAsync(int id)
    {
        var supplierQuote = await _context.SupplierQuotes.FindAsync(id);
        if (supplierQuote != null)
        {
            // Check if this quote is selected as winner for any line
            var isWinner = await _context.PurchaseRequestLines
                .AnyAsync(l => l.WinnerQuoteLineId.HasValue && 
                              _context.SupplierQuoteLines
                                  .Any(sql => sql.Id == l.WinnerQuoteLineId.Value && 
                                             sql.SupplierQuoteId == id));

            if (isWinner)
            {
                throw new InvalidOperationException(
                    "Cannot delete this quote as it is selected as winner for one or more lines");
            }

            supplierQuote.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<SupplierQuote> CalculateTotalsAsync(int supplierQuoteId)
    {
        var supplierQuote = await _context.SupplierQuotes
            .Include(sq => sq.Lines)
            .FirstOrDefaultAsync(sq => sq.Id == supplierQuoteId);

        if (supplierQuote == null)
            throw new ArgumentException($"Supplier quote with ID {supplierQuoteId} not found");

        supplierQuote.TotalAmount = supplierQuote.Lines
            .Where(l => l.IsAvailable)
            .Sum(l => l.TotalPrice);

        await _context.SaveChangesAsync();

        return supplierQuote;
    }

    public async Task<bool> HasQuoteForAllLinesAsync(int purchaseRequestId, int supplierId)
    {
        var purchaseRequestLineIds = await _context.PurchaseRequestLines
            .Where(l => l.PurchaseRequestId == purchaseRequestId)
            .Select(l => l.Id)
            .ToListAsync();

        var quotedLineIds = await _context.SupplierQuoteLines
            .Include(sql => sql.SupplierQuote)
            .Where(sql => sql.SupplierQuote.SupplierId == supplierId && 
                         sql.SupplierQuote.PurchaseRequestId == purchaseRequestId &&
                         sql.SupplierQuote.IsActive &&
                         sql.IsAvailable)
            .Select(sql => sql.PurchaseRequestLineId)
            .Distinct()
            .ToListAsync();

        return purchaseRequestLineIds.All(id => quotedLineIds.Contains(id));
    }
}