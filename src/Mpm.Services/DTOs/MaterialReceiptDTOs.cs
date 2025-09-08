using System.ComponentModel.DataAnnotations;

namespace Mpm.Services.DTOs;

public class CreateMaterialReceiptDto
{
    [Required]
    public int PurchaseOrderId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Number { get; set; } = string.Empty;
    
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string DeliveryNoteNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string ReceivedBy { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Comments { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string PaymentTerms { get; set; } = string.Empty;
    
    public bool IsPartialDelivery { get; set; } = false;
    
    [Required]
    public List<CreateMaterialReceiptLineDto> Lines { get; set; } = new();
}

public class CreateMaterialReceiptLineDto
{
    [Required]
    public int PurchaseOrderLineId { get; set; }
    
    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal ReceivedQuantity { get; set; }
    
    [StringLength(10)]
    public string UnitOfMeasure { get; set; } = "kg";
    
    [StringLength(50)]
    public string HeatNumber { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue)]
    public decimal ActualUnitPrice { get; set; }
    
    public decimal QuantityDeviation { get; set; } = 0;
    
    [StringLength(200)]
    public string DeviationReason { get; set; } = string.Empty;
    
    public bool CreateInventoryLot { get; set; } = true;
    
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;
}

public class MaterialReceiptDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int PurchaseOrderId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string DeliveryNoteNumber { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
    public bool IsPartialDelivery { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<MaterialReceiptLineDto> Lines { get; set; } = new();
}

public class MaterialReceiptLineDto
{
    public int Id { get; set; }
    public int PurchaseOrderLineId { get; set; }
    public string MaterialDescription { get; set; } = string.Empty;
    public string MaterialGrade { get; set; } = string.Empty;
    public string MaterialDimension { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string HeatNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal ActualUnitPrice { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal QuantityDeviation { get; set; }
    public string DeviationReason { get; set; } = string.Empty;
    public List<InventoryLotSummaryDto> InventoryLots { get; set; } = new();
}

public class InventoryLotSummaryDto
{
    public int Id { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Length { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsReserved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MaterialReceiptSearchDto
{
    public int? PurchaseOrderId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? ReceivedBy { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool? IsPartialDelivery { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class InventoryAuditLogDto
{
    public int Id { get; set; }
    public int InventoryLotId { get; set; }
    public string Action { get; set; } = string.Empty;
    public decimal? PreviousQuantity { get; set; }
    public decimal? NewQuantity { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string ReferenceDocument { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}