using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain;
using Mpm.Domain.Entities;

namespace Mpm.Services;

public interface IPriceRequestService
{
    Task<IEnumerable<PriceRequest>> GetAllAsync();
    Task<PriceRequest?> GetByIdAsync(int id);
    Task<PriceRequest?> GetByNumberAsync(string number);
    Task<PriceRequest> CreateAsync(PriceRequest priceRequest);
    Task<PriceRequest> UpdateAsync(PriceRequest priceRequest);
    Task DeleteAsync(int id);
    Task<PriceRequest> ChangeStatusAsync(int id, PriceRequestStatus newStatus);
    Task<PriceRequest> AddLineAsync(int priceRequestId, PriceRequestLine line);
    Task<PriceRequest> RemoveLineAsync(int priceRequestId, int lineId);
    Task<PriceRequest> AddSupplierAsync(int priceRequestId, int supplierId);
    Task<PriceRequest> RemoveSupplierAsync(int priceRequestId, int supplierId);
    Task<IEnumerable<PriceRequest>> GetByStatusAsync(PriceRequestStatus status);
}

public class PriceRequestService : IPriceRequestService
{
    private readonly MpmDbContext _context;

    public PriceRequestService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PriceRequest>> GetAllAsync()
    {
        return await _context.PriceRequests
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.SteelGrade)
            .Include(pr => pr.Suppliers)
                .ThenInclude(s => s.Supplier)
            .OrderByDescending(pr => pr.RequestDate)
            .ToListAsync();
    }

    public async Task<PriceRequest?> GetByIdAsync(int id)
    {
        return await _context.PriceRequests
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.SteelGrade)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.Quotes)
                    .ThenInclude(q => q.Supplier)
            .Include(pr => pr.Suppliers)
                .ThenInclude(s => s.Supplier)
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<PriceRequest?> GetByNumberAsync(string number)
    {
        return await _context.PriceRequests
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.SteelGrade)
            .Include(pr => pr.Suppliers)
                .ThenInclude(s => s.Supplier)
            .FirstOrDefaultAsync(pr => pr.Number == number);
    }

    public async Task<PriceRequest> CreateAsync(PriceRequest priceRequest)
    {
        ValidatePriceRequest(priceRequest);

        if (string.IsNullOrEmpty(priceRequest.Number))
        {
            priceRequest.Number = await GenerateNumberAsync();
        }

        // Ensure status is valid for new requests
        if (priceRequest.Status != PriceRequestStatus.Draft)
        {
            throw new InvalidOperationException("New price requests must start in Draft status.");
        }

        // Validate lines
        foreach (var line in priceRequest.Lines)
        {
            ValidatePriceRequestLine(line);
        }

        _context.PriceRequests.Add(priceRequest);
        await _context.SaveChangesAsync();
        return priceRequest;
    }

    public async Task<PriceRequest> UpdateAsync(PriceRequest priceRequest)
    {
        ValidatePriceRequest(priceRequest);

        var existing = await _context.PriceRequests
            .Include(pr => pr.Lines)
            .FirstOrDefaultAsync(pr => pr.Id == priceRequest.Id);

        if (existing == null)
        {
            throw new ArgumentException("Price request not found");
        }

        // Check if status change is valid
        if (existing.Status != priceRequest.Status)
        {
            ValidateStatusTransition(existing.Status, priceRequest.Status);
        }

        // Update the status-dependent fields
        UpdateStatusDependentFields(priceRequest);

        // Validate lines
        foreach (var line in priceRequest.Lines)
        {
            ValidatePriceRequestLine(line);
        }

        _context.PriceRequests.Update(priceRequest);
        await _context.SaveChangesAsync();
        return priceRequest;
    }

    public async Task DeleteAsync(int id)
    {
        var priceRequest = await _context.PriceRequests.FindAsync(id);
        if (priceRequest != null)
        {
            // Only allow deletion of Draft requests
            if (priceRequest.Status != PriceRequestStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft price requests can be deleted.");
            }

            _context.PriceRequests.Remove(priceRequest);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PriceRequest> ChangeStatusAsync(int id, PriceRequestStatus newStatus)
    {
        var priceRequest = await _context.PriceRequests
            .Include(pr => pr.Lines)
            .FirstOrDefaultAsync(pr => pr.Id == id);

        if (priceRequest == null)
        {
            throw new ArgumentException("Price request not found");
        }

        ValidateStatusTransition(priceRequest.Status, newStatus);

        priceRequest.Status = newStatus;
        UpdateStatusDependentFields(priceRequest);

        await _context.SaveChangesAsync();
        return priceRequest;
    }

    public async Task<PriceRequest> AddLineAsync(int priceRequestId, PriceRequestLine line)
    {
        var priceRequest = await _context.PriceRequests
            .Include(pr => pr.Lines)
            .FirstOrDefaultAsync(pr => pr.Id == priceRequestId);

        if (priceRequest == null)
        {
            throw new ArgumentException("Price request not found");
        }

        // Only allow adding lines to Draft requests
        if (priceRequest.Status != PriceRequestStatus.Draft)
        {
            throw new InvalidOperationException("Lines can only be added to Draft price requests.");
        }

        ValidatePriceRequestLine(line);

        line.PriceRequestId = priceRequestId;
        priceRequest.Lines.Add(line);

        await _context.SaveChangesAsync();
        return priceRequest;
    }

    public async Task<PriceRequest> RemoveLineAsync(int priceRequestId, int lineId)
    {
        var priceRequest = await _context.PriceRequests
            .Include(pr => pr.Lines)
            .FirstOrDefaultAsync(pr => pr.Id == priceRequestId);

        if (priceRequest == null)
        {
            throw new ArgumentException("Price request not found");
        }

        // Only allow removing lines from Draft requests
        if (priceRequest.Status != PriceRequestStatus.Draft)
        {
            throw new InvalidOperationException("Lines can only be removed from Draft price requests.");
        }

        var line = priceRequest.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line != null)
        {
            priceRequest.Lines.Remove(line);
            await _context.SaveChangesAsync();
        }

        return priceRequest;
    }

    public async Task<PriceRequest> AddSupplierAsync(int priceRequestId, int supplierId)
    {
        var priceRequest = await _context.PriceRequests
            .Include(pr => pr.Suppliers)
            .FirstOrDefaultAsync(pr => pr.Id == priceRequestId);

        if (priceRequest == null)
        {
            throw new ArgumentException("Price request not found");
        }

        // Check if supplier already exists
        if (priceRequest.Suppliers.Any(s => s.SupplierId == supplierId))
        {
            throw new InvalidOperationException("Supplier is already invited to this price request.");
        }

        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier == null)
        {
            throw new ArgumentException("Supplier not found");
        }

        var priceRequestSupplier = new PriceRequestSupplier
        {
            PriceRequestId = priceRequestId,
            SupplierId = supplierId,
            InvitedDate = DateTime.UtcNow
        };

        priceRequest.Suppliers.Add(priceRequestSupplier);
        await _context.SaveChangesAsync();
        return priceRequest;
    }

    public async Task<PriceRequest> RemoveSupplierAsync(int priceRequestId, int supplierId)
    {
        var priceRequest = await _context.PriceRequests
            .Include(pr => pr.Suppliers)
            .FirstOrDefaultAsync(pr => pr.Id == priceRequestId);

        if (priceRequest == null)
        {
            throw new ArgumentException("Price request not found");
        }

        var supplierEntry = priceRequest.Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);
        if (supplierEntry != null)
        {
            priceRequest.Suppliers.Remove(supplierEntry);
            await _context.SaveChangesAsync();
        }

        return priceRequest;
    }

    public async Task<IEnumerable<PriceRequest>> GetByStatusAsync(PriceRequestStatus status)
    {
        return await _context.PriceRequests
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.SteelGrade)
            .Include(pr => pr.Suppliers)
                .ThenInclude(s => s.Supplier)
            .Where(pr => pr.Status == status)
            .OrderByDescending(pr => pr.RequestDate)
            .ToListAsync();
    }

    private async Task<string> GenerateNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PR-{year}-";
        
        var lastNumber = await _context.PriceRequests
            .Where(pr => pr.Number.StartsWith(prefix))
            .OrderByDescending(pr => pr.Number)
            .Select(pr => pr.Number)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var lastNumberPart = lastNumber.Substring(prefix.Length);
            if (int.TryParse(lastNumberPart, out int lastNum))
            {
                nextNumber = lastNum + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    private void ValidatePriceRequest(PriceRequest priceRequest)
    {
        if (string.IsNullOrWhiteSpace(priceRequest.RequestedBy))
        {
            throw new InvalidOperationException("RequestedBy is required.");
        }

        if (priceRequest.Lines.Count == 0)
        {
            throw new InvalidOperationException("Price request must have at least one line.");
        }
    }

    private void ValidatePriceRequestLine(PriceRequestLine line)
    {
        if (string.IsNullOrWhiteSpace(line.Description))
        {
            throw new InvalidOperationException("Line description is required.");
        }

        // Validate material type specific fields
        switch (line.MaterialType)
        {
            case MaterialType.Sheet:
                if (!line.LengthMm.HasValue || line.LengthMm <= 0)
                    throw new InvalidOperationException("Length is required for sheet materials.");
                if (!line.WidthMm.HasValue || line.WidthMm <= 0)
                    throw new InvalidOperationException("Width is required for sheet materials.");
                if (!line.ThicknessMm.HasValue || line.ThicknessMm <= 0)
                    throw new InvalidOperationException("Thickness is required for sheet materials.");
                break;

            case MaterialType.Profile:
                if (string.IsNullOrWhiteSpace(line.Dimension))
                    throw new InvalidOperationException("Dimension is required for profile materials.");
                if (line.TotalLength <= 0)
                    throw new InvalidOperationException("Total length must be greater than 0 for profiles.");
                if (line.Pieces <= 0)
                    throw new InvalidOperationException("Number of pieces must be greater than 0 for profiles.");
                break;
        }

        if (line.SteelGradeId.HasValue && line.SteelGradeId <= 0)
        {
            throw new InvalidOperationException("Invalid steel grade.");
        }
    }

    private void ValidateStatusTransition(PriceRequestStatus currentStatus, PriceRequestStatus newStatus)
    {
        // Define valid status transitions
        var validTransitions = new Dictionary<PriceRequestStatus, PriceRequestStatus[]>
        {
            { PriceRequestStatus.Draft, new[] { PriceRequestStatus.Sent, PriceRequestStatus.Canceled } },
            { PriceRequestStatus.Sent, new[] { PriceRequestStatus.Collecting, PriceRequestStatus.Canceled } },
            { PriceRequestStatus.Collecting, new[] { PriceRequestStatus.Completed, PriceRequestStatus.Canceled } },
            { PriceRequestStatus.Completed, new[] { PriceRequestStatus.Canceled } },
            { PriceRequestStatus.Canceled, new PriceRequestStatus[] { } } // Terminal status
        };

        if (!validTransitions.ContainsKey(currentStatus))
        {
            throw new InvalidOperationException($"Invalid current status: {currentStatus}");
        }

        if (!validTransitions[currentStatus].Contains(newStatus))
        {
            throw new InvalidOperationException($"Invalid status transition from {currentStatus} to {newStatus}");
        }
    }

    private void UpdateStatusDependentFields(PriceRequest priceRequest)
    {
        switch (priceRequest.Status)
        {
            case PriceRequestStatus.Sent:
                if (!priceRequest.SentDate.HasValue)
                {
                    priceRequest.SentDate = DateTime.UtcNow;
                }
                break;
        }
    }
}