using Mpm.Data;
using Mpm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public interface IMaterialService
{
    Task<IEnumerable<Material>> GetAllAsync();
    Task<Material?> GetByIdAsync(int id);
    Task<Material> CreateAsync(Material material);
    Task<Material> UpdateAsync(Material material);
    Task DeleteAsync(int id);
}

public interface IWorkOrderService
{
    Task<IEnumerable<WorkOrder>> GetAllAsync();
    Task<WorkOrder?> GetByIdAsync(int id);
    Task<WorkOrder> CreateAsync(WorkOrder workOrder);
    Task<WorkOrder> UpdateAsync(WorkOrder workOrder);
    Task DeleteAsync(int id);
    Task<IEnumerable<WorkOrder>> GetByProjectAsync(int projectId);
}

public interface IInventoryService
{
    Task<IEnumerable<InventoryLot>> GetAllLotsAsync();
    Task<InventoryLot?> GetLotByIdAsync(int id);
    Task<IEnumerable<InventoryLot>> GetAvailableLotsAsync();
    Task<IEnumerable<InventoryLot>> GetReservedLotsAsync();
    Task<MaterialReservation> ReserveMaterialAsync(int lotId, decimal quantity, int? projectId = null, int? workOrderId = null);
    Task UnreserveMaterialAsync(int lotId);
    Task<IEnumerable<InventoryLot>> GetLotsByTypeAsync(string profileType);
    Task<IEnumerable<InventoryLot>> SearchLotsAsync(string searchTerm);
    Task<InventoryLot> CreateLotAsync(InventoryLot lot);
    Task<InventoryLot> UpdateLotAsync(InventoryLot lot);
    Task DeleteLotAsync(int id);
}

public interface IQuotationService
{
    Task<IEnumerable<Quotation>> GetAllAsync();
    Task<Quotation?> GetByIdAsync(int id);
    Task<Quotation> CreateAsync(Quotation quotation);
    Task<Quotation> UpdateAsync(Quotation quotation);
    Task DeleteAsync(int id);
}

public interface IPurchaseOrderService
{
    Task<IEnumerable<PurchaseOrder>> GetAllAsync();
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<PurchaseOrder> CreateAsync(PurchaseOrder purchaseOrder);
    Task<PurchaseOrder> UpdateAsync(PurchaseOrder purchaseOrder);
    Task DeleteAsync(int id);
    Task<PurchaseOrder> ConfirmOrderAsync(int id);
    Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(int supplierId);
}

public interface ISupplierQuoteService
{
    Task<IEnumerable<SupplierQuote>> GetAllAsync();
    Task<SupplierQuote?> GetByIdAsync(int purchaseOrderLineId, int supplierId);
    Task<IEnumerable<SupplierQuote>> GetByPurchaseOrderLineAsync(int purchaseOrderLineId);
    Task<IEnumerable<SupplierQuote>> GetBySupplierAsync(int supplierId);
    Task<SupplierQuote> CreateAsync(SupplierQuote supplierQuote);
    Task<SupplierQuote> UpdateAsync(SupplierQuote supplierQuote);
    Task DeleteAsync(int purchaseOrderLineId, int supplierId);
    Task<IEnumerable<SupplierQuote>> ImportFromCsvAsync(Stream csvStream);
}

public class MaterialService : IMaterialService
{
    private readonly MpmDbContext _context;

    public MaterialService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Material>> GetAllAsync()
    {
        return await _context.Materials
            .OrderBy(m => m.Grade)
            .ThenBy(m => m.Dimension)
            .ToListAsync();
    }

    public async Task<Material?> GetByIdAsync(int id)
    {
        return await _context.Materials.FindAsync(id);
    }

    public async Task<Material> CreateAsync(Material material)
    {
        _context.Materials.Add(material);
        await _context.SaveChangesAsync();
        return material;
    }

    public async Task<Material> UpdateAsync(Material material)
    {
        _context.Materials.Update(material);
        await _context.SaveChangesAsync();
        return material;
    }

    public async Task DeleteAsync(int id)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material != null)
        {
            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();
        }
    }
}

public class WorkOrderService : IWorkOrderService
{
    private readonly MpmDbContext _context;

    public WorkOrderService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkOrder>> GetAllAsync()
    {
        return await _context.WorkOrders
            .Include(w => w.Project)
            .OrderBy(w => w.Priority)
            .ThenBy(w => w.DueDate)
            .ToListAsync();
    }

    public async Task<WorkOrder?> GetByIdAsync(int id)
    {
        return await _context.WorkOrders
            .Include(w => w.Project)
            .Include(w => w.Operations)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<WorkOrder> CreateAsync(WorkOrder workOrder)
    {
        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();
        return workOrder;
    }

    public async Task<WorkOrder> UpdateAsync(WorkOrder workOrder)
    {
        _context.WorkOrders.Update(workOrder);
        await _context.SaveChangesAsync();
        return workOrder;
    }

    public async Task DeleteAsync(int id)
    {
        var workOrder = await _context.WorkOrders.FindAsync(id);
        if (workOrder != null)
        {
            workOrder.Status = Domain.WorkOrderStatus.Cancelled;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<WorkOrder>> GetByProjectAsync(int projectId)
    {
        return await _context.WorkOrders
            .Where(w => w.ProjectId == projectId)
            .Include(w => w.Project)
            .OrderBy(w => w.Priority)
            .ToListAsync();
    }
}

public class InventoryService : IInventoryService
{
    private readonly MpmDbContext _context;

    public InventoryService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InventoryLot>> GetAllLotsAsync()
    {
        return await _context.InventoryLots
            .Include(i => i.Material)
            .Include(i => i.Project)
            .OrderBy(i => i.ArrivalDate)
            .ToListAsync();
    }

    public async Task<InventoryLot?> GetLotByIdAsync(int id)
    {
        return await _context.InventoryLots
            .Include(i => i.Material)
            .Include(i => i.Project)
            .Include(i => i.Certificate)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<InventoryLot>> GetAvailableLotsAsync()
    {
        return await _context.InventoryLots
            .Where(i => !i.IsReserved && i.Quantity > 0)
            .Include(i => i.Material)
            .OrderBy(i => i.ArrivalDate)
            .ToListAsync();
    }

    public async Task<MaterialReservation> ReserveMaterialAsync(int lotId, decimal quantity, int? projectId = null, int? workOrderId = null)
    {
        var lot = await _context.InventoryLots.FindAsync(lotId);
        if (lot == null) throw new ArgumentException("Lot not found");
        if (lot.Quantity < quantity) throw new InvalidOperationException("Insufficient quantity available");

        var reservation = new MaterialReservation
        {
            InventoryLotId = lotId,
            ProjectId = projectId,
            WorkOrderId = workOrderId,
            ReservedQuantity = quantity,
            ReservationDate = DateTime.UtcNow
        };

        _context.MaterialReservations.Add(reservation);
        
        // Update lot reservation status if fully reserved
        var totalReserved = await _context.MaterialReservations
            .Where(r => r.InventoryLotId == lotId)
            .SumAsync(r => r.ReservedQuantity) + quantity;
            
        if (totalReserved >= lot.Quantity)
        {
            lot.IsReserved = true;
        }

        await _context.SaveChangesAsync();
        return reservation;
    }

    public async Task<IEnumerable<InventoryLot>> GetReservedLotsAsync()
    {
        return await _context.InventoryLots
            .Where(i => i.IsReserved)
            .Include(i => i.Material)
            .Include(i => i.Project)
            .OrderBy(i => i.ArrivalDate)
            .ToListAsync();
    }

    public async Task UnreserveMaterialAsync(int lotId)
    {
        var lot = await _context.InventoryLots.FindAsync(lotId);
        if (lot == null) throw new ArgumentException("Lot not found");

        // Remove all reservations for this lot
        var reservations = await _context.MaterialReservations
            .Where(r => r.InventoryLotId == lotId)
            .ToListAsync();

        _context.MaterialReservations.RemoveRange(reservations);
        lot.IsReserved = false;

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<InventoryLot>> GetLotsByTypeAsync(string profileType)
    {
        return await _context.InventoryLots
            .Where(i => i.ProfileType != null && i.ProfileType.Contains(profileType))
            .Include(i => i.Material)
            .Include(i => i.Project)
            .OrderBy(i => i.ArrivalDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryLot>> SearchLotsAsync(string searchTerm)
    {
        return await _context.InventoryLots
            .Where(i => (i.HeatNumber != null && i.HeatNumber.Contains(searchTerm)) ||
                       (i.ProfileType != null && i.ProfileType.Contains(searchTerm)) ||
                       (i.Location != null && i.Location.Contains(searchTerm)) ||
                       (i.SupplierName != null && i.SupplierName.Contains(searchTerm)) ||
                       (i.InvoiceNumber != null && i.InvoiceNumber.Contains(searchTerm)) ||
                       (i.Material != null && i.Material.Grade != null && i.Material.Grade.Contains(searchTerm)) ||
                       (i.Material != null && i.Material.Description != null && i.Material.Description.Contains(searchTerm)))
            .Include(i => i.Material)
            .Include(i => i.Project)
            .OrderBy(i => i.ArrivalDate)
            .ToListAsync();
    }

    public async Task<InventoryLot> CreateLotAsync(InventoryLot lot)
    {
        _context.InventoryLots.Add(lot);
        await _context.SaveChangesAsync();
        return lot;
    }

    public async Task<InventoryLot> UpdateLotAsync(InventoryLot lot)
    {
        _context.InventoryLots.Update(lot);
        await _context.SaveChangesAsync();
        return lot;
    }

    public async Task DeleteLotAsync(int id)
    {
        var lot = await _context.InventoryLots.FindAsync(id);
        if (lot != null)
        {
            _context.InventoryLots.Remove(lot);
            await _context.SaveChangesAsync();
        }
    }
}

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly MpmDbContext _context;

    public PurchaseOrderService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Project)
            .Include(po => po.Lines)
                .ThenInclude(l => l.Material)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Project)
            .Include(po => po.Lines)
                .ThenInclude(l => l.Material)
            .FirstOrDefaultAsync(po => po.Id == id);
    }

    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();
        return purchaseOrder;
    }

    public async Task<PurchaseOrder> UpdateAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Update(purchaseOrder);
        await _context.SaveChangesAsync();
        return purchaseOrder;
    }

    public async Task DeleteAsync(int id)
    {
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
        if (purchaseOrder != null)
        {
            _context.PurchaseOrders.Remove(purchaseOrder);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PurchaseOrder> ConfirmOrderAsync(int id)
    {
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
        if (purchaseOrder == null)
            throw new ArgumentException("Purchase order not found");

        purchaseOrder.IsConfirmed = true;
        await _context.SaveChangesAsync();
        return purchaseOrder;
    }

    public async Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(int supplierId)
    {
        return await _context.PurchaseOrders
            .Where(po => po.SupplierId == supplierId)
            .Include(po => po.Supplier)
            .Include(po => po.Lines)
                .ThenInclude(l => l.Material)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryLot>> GetLotsByTypeAsync(string profileType)
    {
        return await _context.InventoryLots
            .Where(i => i.ProfileType != null && i.ProfileType.Contains(profileType))
            .Include(i => i.Material)
            .Include(i => i.Project)
            .OrderBy(i => i.ArrivalDate)
            .ToListAsync();
    }
}

public class QuotationService : IQuotationService
{
    private readonly MpmDbContext _context;

    public QuotationService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Quotation>> GetAllAsync()
    {
        return await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Project)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<Quotation?> GetByIdAsync(int id)
    {
        return await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Project)
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<Quotation> CreateAsync(Quotation quotation)
    {
        _context.Quotations.Add(quotation);
        await _context.SaveChangesAsync();
        return quotation;
    }

    public async Task<Quotation> UpdateAsync(Quotation quotation)
    {
        _context.Quotations.Update(quotation);
        await _context.SaveChangesAsync();
        return quotation;
    }

    public async Task DeleteAsync(int id)
    {
        var quotation = await _context.Quotations.FindAsync(id);
        if (quotation != null)
        {
            _context.Quotations.Remove(quotation);
            await _context.SaveChangesAsync();
        }
    }
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
            .Include(sq => sq.PurchaseOrderLine)
                .ThenInclude(pol => pol.PurchaseOrder)
            .Include(sq => sq.PurchaseOrderLine)
                .ThenInclude(pol => pol.Material)
            .Include(sq => sq.Supplier)
            .OrderBy(sq => sq.PurchaseOrderLineId)
            .ThenBy(sq => sq.Supplier.Name)
            .ToListAsync();
    }

    public async Task<SupplierQuote?> GetByIdAsync(int purchaseOrderLineId, int supplierId)
    {
        return await _context.SupplierQuotes
            .Include(sq => sq.PurchaseOrderLine)
                .ThenInclude(pol => pol.PurchaseOrder)
            .Include(sq => sq.PurchaseOrderLine)
                .ThenInclude(pol => pol.Material)
            .Include(sq => sq.Supplier)
            .FirstOrDefaultAsync(sq => sq.PurchaseOrderLineId == purchaseOrderLineId && sq.SupplierId == supplierId);
    }

    public async Task<IEnumerable<SupplierQuote>> GetByPurchaseOrderLineAsync(int purchaseOrderLineId)
    {
        return await _context.SupplierQuotes
            .Include(sq => sq.Supplier)
            .Where(sq => sq.PurchaseOrderLineId == purchaseOrderLineId)
            .OrderBy(sq => sq.Supplier.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<SupplierQuote>> GetBySupplierAsync(int supplierId)
    {
        return await _context.SupplierQuotes
            .Include(sq => sq.PurchaseOrderLine)
                .ThenInclude(pol => pol.PurchaseOrder)
            .Include(sq => sq.PurchaseOrderLine)
                .ThenInclude(pol => pol.Material)
            .Where(sq => sq.SupplierId == supplierId)
            .OrderBy(sq => sq.PurchaseOrderLineId)
            .ToListAsync();
    }

    public async Task<SupplierQuote> CreateAsync(SupplierQuote supplierQuote)
    {
        _context.SupplierQuotes.Add(supplierQuote);
        await _context.SaveChangesAsync();
        return supplierQuote;
    }

    public async Task<SupplierQuote> UpdateAsync(SupplierQuote supplierQuote)
    {
        _context.SupplierQuotes.Update(supplierQuote);
        await _context.SaveChangesAsync();
        return supplierQuote;
    }

    public async Task DeleteAsync(int purchaseOrderLineId, int supplierId)
    {
        var quote = await _context.SupplierQuotes
            .FirstOrDefaultAsync(sq => sq.PurchaseOrderLineId == purchaseOrderLineId && sq.SupplierId == supplierId);
        if (quote != null)
        {
            _context.SupplierQuotes.Remove(quote);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<SupplierQuote>> ImportFromCsvAsync(Stream csvStream)
    {
        var quotes = new List<SupplierQuote>();
        var errors = new List<string>();

        using var reader = new StreamReader(csvStream);
        var line = await reader.ReadLineAsync(); // Skip header
        var lineNumber = 1;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            try
            {
                var parts = line.Split(',');
                if (parts.Length < 6)
                {
                    errors.Add($"Line {lineNumber}: Insufficient columns. Expected: PurchaseOrderLineId, SupplierId, Price, Currency, ValidityDate, LeadTimeDays, Notes");
                    continue;
                }

                var purchaseOrderLineId = int.Parse(parts[0].Trim());
                var supplierId = int.Parse(parts[1].Trim());
                var price = decimal.Parse(parts[2].Trim());
                var currency = parts[3].Trim().ToUpperInvariant();
                var validityDate = DateTime.Parse(parts[4].Trim());
                var leadTimeDays = string.IsNullOrWhiteSpace(parts[5].Trim()) ? (int?)null : int.Parse(parts[5].Trim());
                var notes = parts.Length > 6 ? parts[6].Trim() : string.Empty;

                // Validate that the purchase order line exists
                var purchaseOrderLineExists = await _context.PurchaseOrderLines
                    .AnyAsync(pol => pol.Id == purchaseOrderLineId);
                if (!purchaseOrderLineExists)
                {
                    errors.Add($"Line {lineNumber}: Purchase Order Line with ID {purchaseOrderLineId} does not exist");
                    continue;
                }

                // Validate that the supplier exists
                var supplierExists = await _context.Suppliers
                    .AnyAsync(s => s.Id == supplierId);
                if (!supplierExists)
                {
                    errors.Add($"Line {lineNumber}: Supplier with ID {supplierId} does not exist");
                    continue;
                }

                // Validate currency
                if (!Domain.Constants.Currency.IsValidCurrency(currency))
                {
                    errors.Add($"Line {lineNumber}: Invalid currency '{currency}'");
                    continue;
                }

                var quote = new SupplierQuote
                {
                    PurchaseOrderLineId = purchaseOrderLineId,
                    SupplierId = supplierId,
                    Price = price,
                    Currency = currency,
                    ValidityDate = validityDate,
                    LeadTimeDays = leadTimeDays,
                    Notes = notes
                };

                quotes.Add(quote);
            }
            catch (Exception ex)
            {
                errors.Add($"Line {lineNumber}: {ex.Message}");
            }
        }

        if (errors.Any())
        {
            throw new InvalidOperationException($"CSV import failed with errors: {string.Join("; ", errors)}");
        }

        // Save all valid quotes
        foreach (var quote in quotes)
        {
            // Check if quote already exists, update if it does
            var existingQuote = await _context.SupplierQuotes
                .FirstOrDefaultAsync(sq => sq.PurchaseOrderLineId == quote.PurchaseOrderLineId && sq.SupplierId == quote.SupplierId);
            
            if (existingQuote != null)
            {
                existingQuote.Price = quote.Price;
                existingQuote.Currency = quote.Currency;
                existingQuote.ValidityDate = quote.ValidityDate;
                existingQuote.LeadTimeDays = quote.LeadTimeDays;
                existingQuote.Notes = quote.Notes;
                _context.SupplierQuotes.Update(existingQuote);
            }
            else
            {
                _context.SupplierQuotes.Add(quote);
            }
        }

        await _context.SaveChangesAsync();
        return quotes;
    }
}