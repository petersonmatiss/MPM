using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public interface IPriceRequestService
{
    Task<IEnumerable<PriceRequest>> GetAllAsync(PriceRequestStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<PriceRequest?> GetByIdAsync(int id);
    Task<PriceRequest> CreateAsync(PriceRequest priceRequest);
    Task<PriceRequest> UpdateAsync(PriceRequest priceRequest);
    Task DeleteAsync(int id);
    Task<PriceRequest> SubmitAsync(int id);
    Task<bool> IsDuplicateAsync(string number, int? excludeId = null);
    Task<string> GenerateNumberAsync();
}

public class PriceRequestService : IPriceRequestService
{
    private readonly MpmDbContext _context;

    public PriceRequestService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PriceRequest>> GetAllAsync(PriceRequestStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.PriceRequests
            .Include(pr => pr.Lines)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(pr => pr.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(pr => pr.RequestDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(pr => pr.RequestDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(pr => pr.RequestDate)
            .ToListAsync();
    }

    public async Task<PriceRequest?> GetByIdAsync(int id)
    {
        return await _context.PriceRequests
            .Include(pr => pr.Lines)
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<PriceRequest> CreateAsync(PriceRequest priceRequest)
    {
        // Generate unique number if not provided
        if (string.IsNullOrEmpty(priceRequest.Number))
        {
            priceRequest.Number = await GenerateNumberAsync();
        }

        // Check for duplicate number
        if (await IsDuplicateAsync(priceRequest.Number))
        {
            throw new InvalidOperationException($"A price request with number '{priceRequest.Number}' already exists.");
        }

        // Validate line items
        ValidateLineItems(priceRequest.Lines);

        _context.PriceRequests.Add(priceRequest);
        await _context.SaveChangesAsync();
        return priceRequest;
    }

    public async Task<PriceRequest> UpdateAsync(PriceRequest priceRequest)
    {
        // Check for duplicate number (excluding current record)
        if (await IsDuplicateAsync(priceRequest.Number, priceRequest.Id))
        {
            throw new InvalidOperationException($"A price request with number '{priceRequest.Number}' already exists.");
        }

        // Validate line items
        ValidateLineItems(priceRequest.Lines);

        _context.PriceRequests.Update(priceRequest);
        await _context.SaveChangesAsync();
        return priceRequest;
    }

    public async Task DeleteAsync(int id)
    {
        var priceRequest = await _context.PriceRequests.FindAsync(id);
        if (priceRequest != null)
        {
            priceRequest.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PriceRequest> SubmitAsync(int id)
    {
        var priceRequest = await GetByIdAsync(id);
        if (priceRequest == null)
        {
            throw new InvalidOperationException($"Price request with ID {id} not found.");
        }

        if (priceRequest.Status != PriceRequestStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft price requests can be submitted. Current status: {priceRequest.Status}");
        }

        if (priceRequest.Lines?.Any() != true)
        {
            throw new InvalidOperationException("Price request must have at least one line item to submit.");
        }

        // Validate all line items before submission
        ValidateLineItems(priceRequest.Lines);

        priceRequest.Status = PriceRequestStatus.Submitted;
        priceRequest.SubmittedDate = DateTime.UtcNow;

        _context.PriceRequests.Update(priceRequest);
        await _context.SaveChangesAsync();
        return priceRequest;
    }

    public async Task<bool> IsDuplicateAsync(string number, int? excludeId = null)
    {
        var query = _context.PriceRequests
            .Where(pr => pr.Number == number && !pr.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(pr => pr.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<string> GenerateNumberAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"PR-{today:yyyy}{today:MM}";
        
        var lastNumber = await _context.PriceRequests
            .Where(pr => pr.Number.StartsWith(prefix))
            .OrderByDescending(pr => pr.Number)
            .Select(pr => pr.Number)
            .FirstOrDefaultAsync();

        if (lastNumber == null)
        {
            return $"{prefix}-001";
        }

        // Extract the sequential number from the last number
        var lastSequence = lastNumber.Split('-').LastOrDefault();
        if (int.TryParse(lastSequence, out var sequence))
        {
            return $"{prefix}-{(sequence + 1):D3}";
        }

        return $"{prefix}-001";
    }

    private static void ValidateLineItems(ICollection<PriceRequestLine> lines)
    {
        if (lines?.Any() != true)
        {
            return; // Allow empty lines for draft requests
        }

        foreach (var line in lines)
        {
            // Material type is required
            if (!Enum.IsDefined(typeof(MaterialType), line.MaterialType))
            {
                throw new InvalidOperationException("Valid material type is required for each line item.");
            }

            // Dimensions are required
            if (string.IsNullOrWhiteSpace(line.Dimensions))
            {
                throw new InvalidOperationException("Dimensions are required for each line item.");
            }

            // Steel grade is required
            if (string.IsNullOrWhiteSpace(line.SteelGrade))
            {
                throw new InvalidOperationException("Steel grade is required for each line item.");
            }

            // Profile type is mandatory for profiles
            if (line.MaterialType == MaterialType.Profile && string.IsNullOrWhiteSpace(line.ProfileType))
            {
                throw new InvalidOperationException("Profile type is mandatory when material type is Profile.");
            }

            // Validate quantity inputs
            if (line.MaterialType == MaterialType.Profile && line.TotalLength <= 0)
            {
                throw new InvalidOperationException("Total length must be greater than 0 for profile materials.");
            }

            if (line.PieceCount < 0)
            {
                throw new InvalidOperationException("Piece count cannot be negative.");
            }
        }
    }
}