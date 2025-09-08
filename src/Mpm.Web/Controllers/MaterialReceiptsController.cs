using Mpm.Services;
using Mpm.Services.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Mpm.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialReceiptsController : ControllerBase
{
    private readonly IMaterialReceiptService _materialReceiptService;

    public MaterialReceiptsController(IMaterialReceiptService materialReceiptService)
    {
        _materialReceiptService = materialReceiptService;
    }

    /// <summary>
    /// Create a new material receipt
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MaterialReceiptDto>> CreateReceipt([FromBody] CreateMaterialReceiptDto dto)
    {
        try
        {
            var receipt = await _materialReceiptService.CreateReceiptAsync(dto);
            return Ok(receipt);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get material receipt by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MaterialReceiptDto>> GetReceipt(int id)
    {
        try
        {
            var receipt = await _materialReceiptService.GetReceiptByIdAsync(id);
            return Ok(receipt);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Search material receipts with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaterialReceiptDto>>> GetReceipts(
        [FromQuery] int? purchaseOrderId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? receivedBy,
        [FromQuery] string? invoiceNumber,
        [FromQuery] bool? isPartialDelivery,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var searchDto = new MaterialReceiptSearchDto
            {
                PurchaseOrderId = purchaseOrderId,
                FromDate = fromDate,
                ToDate = toDate,
                ReceivedBy = receivedBy,
                InvoiceNumber = invoiceNumber,
                IsPartialDelivery = isPartialDelivery,
                Page = page,
                PageSize = pageSize
            };

            var receipts = await _materialReceiptService.GetReceiptsAsync(searchDto);
            return Ok(receipts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all material receipts for a specific purchase order
    /// </summary>
    [HttpGet("purchase-order/{purchaseOrderId}")]
    public async Task<ActionResult<IEnumerable<MaterialReceiptDto>>> GetReceiptsByPurchaseOrder(int purchaseOrderId)
    {
        try
        {
            var receipts = await _materialReceiptService.GetReceiptsByPurchaseOrderAsync(purchaseOrderId);
            return Ok(receipts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a material receipt (only if no inventory is reserved)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteReceipt(int id)
    {
        try
        {
            var result = await _materialReceiptService.DeleteReceiptAsync(id);
            if (!result)
                return NotFound("Material receipt not found");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get audit log for a specific inventory lot
    /// </summary>
    [HttpGet("audit/{inventoryLotId}")]
    public async Task<ActionResult<IEnumerable<InventoryAuditLogDto>>> GetAuditLog(int inventoryLotId)
    {
        try
        {
            var auditLogs = await _materialReceiptService.GetInventoryAuditLogAsync(inventoryLotId);
            return Ok(auditLogs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all inventory audit logs with optional date filtering
    /// </summary>
    [HttpGet("audit")]
    public async Task<ActionResult<IEnumerable<InventoryAuditLogDto>>> GetAllAuditLogs(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var auditLogs = await _materialReceiptService.GetAllInventoryAuditLogsAsync(fromDate, toDate);
            return Ok(auditLogs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}