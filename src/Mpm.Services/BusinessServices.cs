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
    Task<IEnumerable<string>> GetPopularDimensionsAsync(string profileType, int limit = 10);
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

    public async Task<IEnumerable<string>> GetPopularDimensionsAsync(string profileType, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(profileType))
        {
            return Enumerable.Empty<string>();
        }

        // Get dimensions from inventory lots and profiles for the given profile type
        var inventoryDimensions = await _context.InventoryLots
            .Where(i => i.ProfileType != null && i.ProfileType.Contains(profileType))
            .Select(i => i.ProfileType!)
            .Where(pt => !string.IsNullOrEmpty(pt))
            .ToListAsync();

        var profileDimensions = await _context.Profiles
            .Include(p => p.ProfileType)
            .Where(p => p.ProfileType != null && p.ProfileType.Code == profileType && !string.IsNullOrEmpty(p.Dimension))
            .Select(p => p.Dimension!)
            .ToListAsync();

        // Combine and count frequencies
        var allDimensions = inventoryDimensions.Concat(profileDimensions);
        
        return allDimensions
            .GroupBy(d => d)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key)
            .ToList();
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