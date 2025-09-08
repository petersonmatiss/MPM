using Microsoft.AspNetCore.Mvc;
using Mpm.Domain;
using Mpm.Services;
using Mpm.Services.DTOs;

namespace Mpm.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<IEnumerable<InventoryReportDto>>> GetInventoryReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] string? materialGrade = null,
        [FromQuery] string? profileType = null,
        [FromQuery] bool? lowStockOnly = null)
    {
        var filter = new ReportFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            SupplierId = supplierId,
            MaterialGrade = materialGrade,
            ProfileType = profileType,
            LowStockOnly = lowStockOnly
        };

        var report = await _reportingService.GetInventoryReportAsync(filter);
        return Ok(report);
    }

    [HttpGet("inventory/summary")]
    public async Task<ActionResult<InventoryLevelSummaryDto>> GetInventorySummary()
    {
        var summary = await _reportingService.GetInventoryLevelSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("price-requests")]
    public async Task<ActionResult<IEnumerable<PriceRequestReportDto>>> GetPriceRequestReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] PriceRequestStatus? status = null)
    {
        var filter = new ReportFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            PriceRequestStatus = status
        };

        var report = await _reportingService.GetPriceRequestReportAsync(filter);
        return Ok(report);
    }

    [HttpGet("purchase-orders")]
    public async Task<ActionResult<IEnumerable<PurchaseOrderReportDto>>> GetPurchaseOrderReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] int? projectId = null,
        [FromQuery] bool? isConfirmed = null)
    {
        var filter = new ReportFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            SupplierId = supplierId,
            ProjectId = projectId,
            IsConfirmed = isConfirmed
        };

        var report = await _reportingService.GetPurchaseOrderReportAsync(filter);
        return Ok(report);
    }

    [HttpGet("supplier-performance")]
    public async Task<ActionResult<IEnumerable<SupplierPerformanceDto>>> GetSupplierPerformanceReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? supplierId = null)
    {
        var filter = new ReportFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            SupplierId = supplierId
        };

        var report = await _reportingService.GetSupplierPerformanceReportAsync(filter);
        return Ok(report);
    }

    [HttpGet("supplier-performance/{supplierId}")]
    public async Task<ActionResult<SupplierPerformanceDto>> GetSupplierPerformanceById(int supplierId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var filter = new ReportFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        var report = await _reportingService.GetSupplierPerformanceByIdAsync(supplierId, filter);
        if (report == null)
            return NotFound();

        return Ok(report);
    }

    [HttpGet("cost-trends")]
    public async Task<ActionResult<IEnumerable<MaterialCostTrendDto>>> GetMaterialCostTrends(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] string? materialGrade = null,
        [FromQuery] string? profileType = null)
    {
        var filter = new ReportFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            SupplierId = supplierId,
            MaterialGrade = materialGrade,
            ProfileType = profileType
        };

        var report = await _reportingService.GetMaterialCostTrendsAsync(filter);
        return Ok(report);
    }

    [HttpGet("cost-trends/supplier/{supplierId}")]
    public async Task<ActionResult<IEnumerable<MaterialCostTrendDto>>> GetMaterialCostTrendsBySupplier(int supplierId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? materialGrade = null,
        [FromQuery] string? profileType = null)
    {
        var filter = new ReportFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            MaterialGrade = materialGrade,
            ProfileType = profileType
        };

        var report = await _reportingService.GetMaterialCostTrendsBySupplierAsync(supplierId, filter);
        return Ok(report);
    }
}