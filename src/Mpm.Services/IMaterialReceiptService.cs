using Mpm.Services.DTOs;

namespace Mpm.Services;

public interface IMaterialReceiptService
{
    Task<MaterialReceiptDto> CreateReceiptAsync(CreateMaterialReceiptDto dto);
    Task<MaterialReceiptDto> GetReceiptByIdAsync(int id);
    Task<IEnumerable<MaterialReceiptDto>> GetReceiptsAsync(MaterialReceiptSearchDto searchDto);
    Task<IEnumerable<MaterialReceiptDto>> GetReceiptsByPurchaseOrderAsync(int purchaseOrderId);
    Task<bool> DeleteReceiptAsync(int id);
    Task<IEnumerable<InventoryAuditLogDto>> GetInventoryAuditLogAsync(int inventoryLotId);
    Task<IEnumerable<InventoryAuditLogDto>> GetAllInventoryAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null);
}