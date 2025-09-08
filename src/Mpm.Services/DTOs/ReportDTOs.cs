using Mpm.Domain;

namespace Mpm.Services.DTOs;

public class InventoryReportDto
{
    public string MaterialGrade { get; set; } = string.Empty;
    public string ProfileType { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public int LotCount { get; set; }
    public decimal AverageUnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsLowStock { get; set; }
    public string PrimarySupplier { get; set; } = string.Empty;
    public DateTime? OldestLotDate { get; set; }
    public DateTime? NewestLotDate { get; set; }
}

public class PriceRequestReportDto
{
    public string Number { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PriceRequestStatus Status { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int DaysInStatus { get; set; }
    public bool IsOverdue { get; set; }
}

public class PurchaseOrderReportDto
{
    public string Number { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsDelivered { get; set; }
    public int LineCount { get; set; }
    public int DaysToDelivery { get; set; }
    public bool IsOverdue { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}

public class SupplierPerformanceDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalOrderValue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double AverageDeliveryDays { get; set; }
    public int OnTimeDeliveries { get; set; }
    public int LateDeliveries { get; set; }
    public double OnTimeDeliveryRate { get; set; }
    public decimal AveragePricePerKg { get; set; }
    public int QualityIssues { get; set; }
    public double QualityScore { get; set; }
    public DateTime FirstOrderDate { get; set; }
    public DateTime LastOrderDate { get; set; }
    public List<MaterialCostTrendDto> MaterialCostTrends { get; set; } = new();
}

public class MaterialCostTrendDto
{
    public DateTime Date { get; set; }
    public string MaterialGrade { get; set; } = string.Empty;
    public string ProfileType { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class InventoryLevelSummaryDto
{
    public decimal TotalInventoryValue { get; set; }
    public int TotalLots { get; set; }
    public int LowStockItems { get; set; }
    public int ReservedLots { get; set; }
    public decimal ReservedValue { get; set; }
    public int UniqueMaterials { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ReportFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? SupplierId { get; set; }
    public int? ProjectId { get; set; }
    public string? MaterialGrade { get; set; }
    public string? ProfileType { get; set; }
    public PriceRequestStatus? PriceRequestStatus { get; set; }
    public bool? IsConfirmed { get; set; }
    public bool? LowStockOnly { get; set; }
}