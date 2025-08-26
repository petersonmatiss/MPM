using Mpm.Data;
using Mpm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public interface ISupplierService
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task<Supplier> UpdateAsync(Supplier supplier);
    Task DeleteAsync(int id);
    Task<bool> IsDuplicateAsync(string name, string vatNumber, int? excludeId = null);
}

public class SupplierService : ISupplierService
{
    private readonly MpmDbContext _context;

    public SupplierService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        return await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        // Check for duplicates
        if (await IsDuplicateAsync(supplier.Name, supplier.VatNumber))
        {
            throw new InvalidOperationException($"A supplier with name '{supplier.Name}' and VAT number '{supplier.VatNumber}' already exists.");
        }

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier> UpdateAsync(Supplier supplier)
    {
        // Check for duplicates excluding current supplier
        if (await IsDuplicateAsync(supplier.Name, supplier.VatNumber, supplier.Id))
        {
            throw new InvalidOperationException($"A supplier with name '{supplier.Name}' and VAT number '{supplier.VatNumber}' already exists.");
        }

        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            // Soft delete by setting IsActive to false
            supplier.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsDuplicateAsync(string name, string vatNumber, int? excludeId = null)
    {
        var query = _context.Suppliers
            .Where(s => s.Name.ToLower() == name.ToLower() && 
                       s.VatNumber.ToLower() == vatNumber.ToLower() &&
                       s.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}