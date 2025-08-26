using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public interface IInvoiceService
{
    Task<IEnumerable<Invoice>> GetAllAsync(int? supplierId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice> UpdateAsync(Invoice invoice);
    Task DeleteAsync(int id);
    Task<bool> IsDuplicateAsync(string number, int? excludeId = null);
}

public class InvoiceService : IInvoiceService
{
    private readonly MpmDbContext _context;

    public InvoiceService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync(int? supplierId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .AsQueryable();

        if (supplierId.HasValue)
        {
            query = query.Where(i => i.SupplierId == supplierId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenBy(i => i.Number)
            .ToListAsync();
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        // Validate currency
        if (!Constants.Currency.IsValidCurrency(invoice.Currency))
        {
            throw new InvalidOperationException($"Currency '{invoice.Currency}' is not a valid ISO 4217 currency code.");
        }

        // Check for duplicate invoice number
        if (await IsDuplicateAsync(invoice.Number))
        {
            throw new InvalidOperationException($"An invoice with number '{invoice.Number}' already exists.");
        }

        // Calculate totals
        CalculateTotals(invoice);

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice> UpdateAsync(Invoice invoice)
    {
        // Validate currency
        if (!Constants.Currency.IsValidCurrency(invoice.Currency))
        {
            throw new InvalidOperationException($"Currency '{invoice.Currency}' is not a valid ISO 4217 currency code.");
        }

        // Check for duplicate invoice number excluding current invoice
        if (await IsDuplicateAsync(invoice.Number, invoice.Id))
        {
            throw new InvalidOperationException($"An invoice with number '{invoice.Number}' already exists.");
        }

        // Calculate totals
        CalculateTotals(invoice);

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task DeleteAsync(int id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice != null)
        {
            // Soft delete by setting IsDeleted to true
            invoice.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsDuplicateAsync(string number, int? excludeId = null)
    {
        var query = _context.Invoices
            .Where(i => i.Number == number && !i.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(i => i.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private static void CalculateTotals(Invoice invoice)
    {
        if (invoice.Lines?.Any() == true)
        {
            foreach (var line in invoice.Lines)
            {
                line.TotalPrice = line.Quantity * line.UnitPrice;
            }

            invoice.SubTotal = invoice.Lines.Sum(l => l.TotalPrice);
            invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
        }
    }
}