using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain;
using Mpm.Services.DTOs;

namespace Mpm.Services;

public class ReportingService : IReportingService
{
    private readonly MpmDbContext _context;
    private const decimal LowStockThreshold = 50.0m;

    public ReportingService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InventoryReportDto>> GetInventoryReportAsync(ReportFilterDto? filter = null)
    {
        var query = _context.InventoryLots
            .Include(i => i.Material)
            .Include(i => i.Reservations)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.FromDate.HasValue)
                query = query.Where(i => i.ArrivalDate >= filter.FromDate.Value);
            
            if (filter.ToDate.HasValue)
                query = query.Where(i => i.ArrivalDate <= filter.ToDate.Value);
            
            if (!string.IsNullOrEmpty(filter.MaterialGrade))
                query = query.Where(i => i.Material.Grade.Contains(filter.MaterialGrade));
            
            if (!string.IsNullOrEmpty(filter.ProfileType))
                query = query.Where(i => i.ProfileType.Contains(filter.ProfileType));
            
            if (filter.LowStockOnly == true)
                query = query.Where(i => i.Quantity < LowStockThreshold);
        }

        var inventoryData = await query.ToListAsync();

        var groupedData = inventoryData
            .GroupBy(i => new { 
                MaterialGrade = i.Material.Grade, 
                ProfileType = i.ProfileType, 
                Dimension = i.Material.Dimension 
            })
            .Select(g => new InventoryReportDto
            {
                MaterialGrade = g.Key.MaterialGrade,
                ProfileType = g.Key.ProfileType,
                Dimension = g.Key.Dimension,
                TotalQuantity = g.Sum(i => i.Quantity),
                ReservedQuantity = g.Sum(i => i.Reservations.Sum(r => r.ReservedQuantity)),
                AvailableQuantity = g.Sum(i => i.Quantity) - g.Sum(i => i.Reservations.Sum(r => r.ReservedQuantity)),
                LotCount = g.Count(),
                AverageUnitPrice = g.Average(i => i.UnitPrice),
                TotalValue = g.Sum(i => i.Quantity * i.UnitPrice),
                IsLowStock = g.Sum(i => i.Quantity) < LowStockThreshold,
                PrimarySupplier = g.GroupBy(i => i.SupplierName)
                    .OrderByDescending(sg => sg.Sum(i => i.Quantity))
                    .FirstOrDefault()?.Key ?? "",
                OldestLotDate = g.Min(i => i.ArrivalDate),
                NewestLotDate = g.Max(i => i.ArrivalDate)
            })
            .OrderBy(r => r.MaterialGrade)
            .ThenBy(r => r.ProfileType);

        return groupedData;
    }

    public async Task<InventoryLevelSummaryDto> GetInventoryLevelSummaryAsync()
    {
        var inventoryLots = await _context.InventoryLots
            .Include(i => i.Reservations)
            .ToListAsync();

        var totalValue = inventoryLots.Sum(i => i.Quantity * i.UnitPrice);
        var reservedValue = inventoryLots.Sum(i => i.Reservations.Sum(r => r.ReservedQuantity * i.UnitPrice));
        var lowStockItems = inventoryLots.Count(i => i.Quantity < LowStockThreshold);
        var uniqueMaterials = inventoryLots.Select(i => new { i.Material.Grade, i.ProfileType, i.Material.Dimension }).Distinct().Count();

        return new InventoryLevelSummaryDto
        {
            TotalInventoryValue = totalValue,
            TotalLots = inventoryLots.Count,
            LowStockItems = lowStockItems,
            ReservedLots = inventoryLots.Count(i => i.IsReserved),
            ReservedValue = reservedValue,
            UniqueMaterials = uniqueMaterials
        };
    }

    public async Task<IEnumerable<PriceRequestReportDto>> GetPriceRequestReportAsync(ReportFilterDto? filter = null)
    {
        var query = _context.PriceRequests
            .Include(pr => pr.Lines)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.FromDate.HasValue)
                query = query.Where(pr => pr.RequestDate >= filter.FromDate.Value);
            
            if (filter.ToDate.HasValue)
                query = query.Where(pr => pr.RequestDate <= filter.ToDate.Value);
            
            if (filter.PriceRequestStatus.HasValue)
                query = query.Where(pr => pr.Status == filter.PriceRequestStatus.Value);
        }

        var priceRequests = await query.ToListAsync();

        return priceRequests.Select(pr => new PriceRequestReportDto
        {
            Number = pr.Number,
            Description = pr.Description,
            Status = pr.Status,
            RequestDate = pr.RequestDate,
            SubmittedDate = pr.SubmittedDate,
            RequestedBy = pr.RequestedBy,
            LineCount = pr.Lines.Count,
            DaysInStatus = (DateTime.UtcNow - (pr.SubmittedDate ?? pr.RequestDate)).Days,
            IsOverdue = pr.Status == PriceRequestStatus.Submitted && 
                       (DateTime.UtcNow - (pr.SubmittedDate ?? pr.RequestDate)).Days > 5
        }).OrderByDescending(pr => pr.RequestDate);
    }

    public async Task<IEnumerable<PurchaseOrderReportDto>> GetPurchaseOrderReportAsync(ReportFilterDto? filter = null)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Project)
            .Include(po => po.Lines)
            .Include(po => po.GoodsReceiptNotes)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.FromDate.HasValue)
                query = query.Where(po => po.OrderDate >= filter.FromDate.Value);
            
            if (filter.ToDate.HasValue)
                query = query.Where(po => po.OrderDate <= filter.ToDate.Value);
            
            if (filter.SupplierId.HasValue)
                query = query.Where(po => po.SupplierId == filter.SupplierId.Value);
            
            if (filter.ProjectId.HasValue)
                query = query.Where(po => po.ProjectId == filter.ProjectId.Value);
            
            if (filter.IsConfirmed.HasValue)
                query = query.Where(po => po.IsConfirmed == filter.IsConfirmed.Value);
        }

        var purchaseOrders = await query.ToListAsync();

        return purchaseOrders.Select(po => new PurchaseOrderReportDto
        {
            Number = po.Number,
            SupplierName = po.Supplier.Name,
            OrderDate = po.OrderDate,
            DeliveryDate = po.DeliveryDate,
            TotalValue = po.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100)),
            IsConfirmed = po.IsConfirmed,
            IsDelivered = po.GoodsReceiptNotes.Any(),
            LineCount = po.Lines.Count,
            DaysToDelivery = po.DeliveryDate.HasValue 
                ? (po.DeliveryDate.Value - po.OrderDate).Days 
                : (DateTime.UtcNow - po.OrderDate).Days,
            IsOverdue = po.DeliveryDate.HasValue && po.DeliveryDate.Value < DateTime.UtcNow && !po.GoodsReceiptNotes.Any(),
            ProjectName = po.Project?.Name ?? ""
        }).OrderByDescending(po => po.OrderDate);
    }

    public async Task<IEnumerable<SupplierPerformanceDto>> GetSupplierPerformanceReportAsync(ReportFilterDto? filter = null)
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.PurchaseOrders)
                .ThenInclude(po => po.Lines)
            .Include(s => s.PurchaseOrders)
                .ThenInclude(po => po.GoodsReceiptNotes)
            .Where(s => s.IsActive)
            .ToListAsync();

        var result = new List<SupplierPerformanceDto>();

        foreach (var supplier in suppliers)
        {
            var orders = supplier.PurchaseOrders.AsQueryable();
            
            // Apply date filters
            if (filter?.FromDate.HasValue == true)
                orders = orders.Where(po => po.OrderDate >= filter.FromDate.Value);
            
            if (filter?.ToDate.HasValue == true)
                orders = orders.Where(po => po.OrderDate <= filter.ToDate.Value);

            var ordersList = orders.ToList();
            
            if (!ordersList.Any()) continue;

            var deliveredOrders = ordersList.Where(po => po.GoodsReceiptNotes.Any()).ToList();
            var onTimeDeliveries = deliveredOrders.Count(po => 
                po.DeliveryDate.HasValue && 
                po.GoodsReceiptNotes.Any(grn => grn.ReceiptDate <= po.DeliveryDate.Value));

            var materialCosts = await GetMaterialCostTrendsBySupplierAsync(supplier.Id, filter);

            result.Add(new SupplierPerformanceDto
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                TotalOrders = ordersList.Count,
                TotalOrderValue = ordersList.Sum(po => po.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100))),
                AverageOrderValue = ordersList.Any() ? ordersList.Average(po => po.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100))) : 0,
                AverageDeliveryDays = deliveredOrders.Any() 
                    ? deliveredOrders.Where(po => po.DeliveryDate.HasValue)
                        .Average(po => (po.GoodsReceiptNotes.Min(grn => grn.ReceiptDate) - po.OrderDate).TotalDays)
                    : 0,
                OnTimeDeliveries = onTimeDeliveries,
                LateDeliveries = deliveredOrders.Count - onTimeDeliveries,
                OnTimeDeliveryRate = deliveredOrders.Any() ? (double)onTimeDeliveries / deliveredOrders.Count * 100 : 100,
                AveragePricePerKg = ordersList.SelectMany(po => po.Lines).Any() 
                    ? ordersList.SelectMany(po => po.Lines).Average(l => l.UnitPrice) 
                    : 0,
                QualityIssues = 0, // TODO: Implement when NCR system is connected to suppliers
                QualityScore = 95.0, // Placeholder - should be calculated based on NCRs
                FirstOrderDate = ordersList.Min(po => po.OrderDate),
                LastOrderDate = ordersList.Max(po => po.OrderDate),
                MaterialCostTrends = materialCosts.ToList()
            });
        }

        return result.OrderByDescending(s => s.TotalOrderValue);
    }

    public async Task<SupplierPerformanceDto?> GetSupplierPerformanceByIdAsync(int supplierId, ReportFilterDto? filter = null)
    {
        var allPerformance = await GetSupplierPerformanceReportAsync(filter);
        return allPerformance.FirstOrDefault(s => s.SupplierId == supplierId);
    }

    public async Task<IEnumerable<MaterialCostTrendDto>> GetMaterialCostTrendsAsync(ReportFilterDto? filter = null)
    {
        var query = _context.InventoryLots
            .Include(i => i.Material)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.FromDate.HasValue)
                query = query.Where(i => i.ArrivalDate >= filter.FromDate.Value);
            
            if (filter.ToDate.HasValue)
                query = query.Where(i => i.ArrivalDate <= filter.ToDate.Value);
            
            if (!string.IsNullOrEmpty(filter.MaterialGrade))
                query = query.Where(i => i.Material.Grade.Contains(filter.MaterialGrade));
            
            if (!string.IsNullOrEmpty(filter.ProfileType))
                query = query.Where(i => i.ProfileType.Contains(filter.ProfileType));
            
            if (filter.SupplierId.HasValue)
            {
                var supplierName = await _context.Suppliers
                    .Where(s => s.Id == filter.SupplierId.Value)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
                
                if (!string.IsNullOrEmpty(supplierName))
                    query = query.Where(i => i.SupplierName == supplierName);
            }
        }

        var inventoryLots = await query.ToListAsync();

        return inventoryLots.Select(i => new MaterialCostTrendDto
        {
            Date = i.ArrivalDate,
            MaterialGrade = i.Material.Grade,
            ProfileType = i.ProfileType,
            Dimension = i.Material.Dimension,
            UnitPrice = i.UnitPrice,
            SupplierName = i.SupplierName,
            Quantity = i.Quantity
        }).OrderBy(t => t.Date);
    }

    public async Task<IEnumerable<MaterialCostTrendDto>> GetMaterialCostTrendsBySupplierAsync(int supplierId, ReportFilterDto? filter = null)
    {
        var supplierName = await _context.Suppliers
            .Where(s => s.Id == supplierId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(supplierName))
            return Enumerable.Empty<MaterialCostTrendDto>();

        var newFilter = new ReportFilterDto
        {
            FromDate = filter?.FromDate,
            ToDate = filter?.ToDate,
            MaterialGrade = filter?.MaterialGrade,
            ProfileType = filter?.ProfileType,
            SupplierId = supplierId
        };

        return await GetMaterialCostTrendsAsync(newFilter);
    }
}