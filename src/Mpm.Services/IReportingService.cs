using Mpm.Services.DTOs;

namespace Mpm.Services;

public interface IReportingService
{
    // Inventory Reports
    Task<IEnumerable<InventoryReportDto>> GetInventoryReportAsync(ReportFilterDto? filter = null);
    Task<InventoryLevelSummaryDto> GetInventoryLevelSummaryAsync();
    
    // Price Request Reports
    Task<IEnumerable<PriceRequestReportDto>> GetPriceRequestReportAsync(ReportFilterDto? filter = null);
    
    // Purchase Order Reports
    Task<IEnumerable<PurchaseOrderReportDto>> GetPurchaseOrderReportAsync(ReportFilterDto? filter = null);
    
    // Supplier Performance Analysis
    Task<IEnumerable<SupplierPerformanceDto>> GetSupplierPerformanceReportAsync(ReportFilterDto? filter = null);
    Task<SupplierPerformanceDto?> GetSupplierPerformanceByIdAsync(int supplierId, ReportFilterDto? filter = null);
    
    // Material Cost Trends
    Task<IEnumerable<MaterialCostTrendDto>> GetMaterialCostTrendsAsync(ReportFilterDto? filter = null);
    Task<IEnumerable<MaterialCostTrendDto>> GetMaterialCostTrendsBySupplierAsync(int supplierId, ReportFilterDto? filter = null);
}