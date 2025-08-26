using Mpm.Data;
using Mpm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public interface ISheetService
{
    Task<IEnumerable<Sheet>> GetAllAsync(int? thicknessMm = null, string? sizeFilter = null);
    Task<Sheet?> GetByIdAsync(int id);
    Task<Sheet> CreateAsync(Sheet sheet);
    Task<Sheet> UpdateAsync(Sheet sheet);
    Task DeleteAsync(int id);
    Task<bool> CanDeleteAsync(int id);
    Task<IEnumerable<Sheet>> GetRemnantSheetsAsync(int? thicknessMm = null, string? sizeFilter = null);
}

public class SheetService : ISheetService
{
    private readonly MpmDbContext _context;

    public SheetService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Sheet>> GetAllAsync(int? thicknessMm = null, string? sizeFilter = null)
    {
        var query = _context.Sheets.AsQueryable();

        // Apply thickness filter
        if (thicknessMm.HasValue)
        {
            query = query.Where(s => s.ThicknessMm == thicknessMm.Value);
        }

        // Apply size filter (contains search on length x width)
        if (!string.IsNullOrEmpty(sizeFilter))
        {
            query = query.Where(s => 
                EF.Functions.Like($"{s.LengthMm}x{s.WidthMm}", $"%{sizeFilter}%") ||
                EF.Functions.Like($"{s.WidthMm}x{s.LengthMm}", $"%{sizeFilter}%"));
        }

        return await query
            .OrderBy(s => s.Grade)
            .ThenBy(s => s.ThicknessMm)
            .ThenBy(s => s.LengthMm)
            .ToListAsync();
    }

    public async Task<Sheet?> GetByIdAsync(int id)
    {
        return await _context.Sheets
            .Include(s => s.InvoiceLine)
            .Include(s => s.Project)
            .Include(s => s.Certificate)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Sheet> CreateAsync(Sheet sheet)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(sheet.SheetId))
        {
            throw new InvalidOperationException("SheetId is required.");
        }

        if (string.IsNullOrEmpty(sheet.Grade))
        {
            throw new InvalidOperationException("Grade is required.");
        }

        if (sheet.LengthMm <= 0 || sheet.WidthMm <= 0 || sheet.ThicknessMm <= 0)
        {
            throw new InvalidOperationException("Length, Width, and Thickness must be greater than 0.");
        }

        // Check for duplicate SheetId
        var existingSheet = await _context.Sheets
            .FirstOrDefaultAsync(s => s.SheetId == sheet.SheetId);
        
        if (existingSheet != null)
        {
            throw new InvalidOperationException($"A sheet with ID '{sheet.SheetId}' already exists.");
        }

        _context.Sheets.Add(sheet);
        await _context.SaveChangesAsync();
        return sheet;
    }

    public async Task<Sheet> UpdateAsync(Sheet sheet)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(sheet.SheetId))
        {
            throw new InvalidOperationException("SheetId is required.");
        }

        if (string.IsNullOrEmpty(sheet.Grade))
        {
            throw new InvalidOperationException("Grade is required.");
        }

        if (sheet.LengthMm <= 0 || sheet.WidthMm <= 0 || sheet.ThicknessMm <= 0)
        {
            throw new InvalidOperationException("Length, Width, and Thickness must be greater than 0.");
        }

        // Check for duplicate SheetId excluding current sheet
        var existingSheet = await _context.Sheets
            .FirstOrDefaultAsync(s => s.SheetId == sheet.SheetId && s.Id != sheet.Id);
        
        if (existingSheet != null)
        {
            throw new InvalidOperationException($"A sheet with ID '{sheet.SheetId}' already exists.");
        }

        _context.Sheets.Update(sheet);
        await _context.SaveChangesAsync();
        return sheet;
    }

    public async Task DeleteAsync(int id)
    {
        var sheet = await _context.Sheets.FindAsync(id);
        if (sheet != null)
        {
            // Check if sheet can be deleted
            if (!await CanDeleteAsync(id))
            {
                throw new InvalidOperationException("Cannot delete sheet that has been used.");
            }

            // Soft delete by setting IsDeleted to true (inherited from BaseEntity)
            sheet.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> CanDeleteAsync(int id)
    {
        var sheet = await _context.Sheets
            .Include(s => s.Usages)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sheet == null)
            return false;

        // Cannot delete if sheet is marked as used or has usage records
        return !sheet.IsUsed && !sheet.Usages.Any();
    }

    public async Task<IEnumerable<Sheet>> GetRemnantSheetsAsync(int? thicknessMm = null, string? sizeFilter = null)
    {
        // For now, consider sheets with usage that generated remnants as "remnant sheets"
        // This could be enhanced based on specific business requirements
        var query = _context.Sheets
            .Include(s => s.Usages)
            .Where(s => s.Usages.Any(u => u.GeneratedRemnants));

        // Apply thickness filter
        if (thicknessMm.HasValue)
        {
            query = query.Where(s => s.ThicknessMm == thicknessMm.Value);
        }

        // Apply size filter
        if (!string.IsNullOrEmpty(sizeFilter))
        {
            query = query.Where(s => 
                EF.Functions.Like($"{s.LengthMm}x{s.WidthMm}", $"%{sizeFilter}%") ||
                EF.Functions.Like($"{s.WidthMm}x{s.LengthMm}", $"%{sizeFilter}%"));
        }

        return await query
            .OrderBy(s => s.Grade)
            .ThenBy(s => s.ThicknessMm)
            .ThenBy(s => s.LengthMm)
            .ToListAsync();
    }
}