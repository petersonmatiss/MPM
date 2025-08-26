namespace Mpm.Domain.Entities;

public class GoodsReceiptNote : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public int PurchaseOrderId { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public string DeliveryNoteNumber { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;

    // Navigation properties
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual ICollection<GoodsReceiptNoteLine> Lines { get; set; } = new List<GoodsReceiptNoteLine>();
}

public class GoodsReceiptNoteLine : TenantEntity
{
    public int GoodsReceiptNoteId { get; set; }
    public int PurchaseOrderLineId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    public string HeatNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual GoodsReceiptNote GoodsReceiptNote { get; set; } = null!;
    public virtual PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
    public virtual ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();
}

public class InventoryLot : TenantEntity
{
    public int MaterialId { get; set; }
    public int? ProjectId { get; set; }
    public int? GoodsReceiptNoteLineId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Length { get; set; }
    public string HeatNumber { get; set; } = string.Empty;
    public int? CertificateId { get; set; }
    public string ProfileType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime ArrivalDate { get; set; } = DateTime.UtcNow;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsReserved { get; set; } = false;

    // Navigation properties
    public virtual Material Material { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual GoodsReceiptNoteLine? GoodsReceiptNoteLine { get; set; }
    public virtual Certificate? Certificate { get; set; }
    public virtual ICollection<MaterialReservation> Reservations { get; set; } = new List<MaterialReservation>();
}

public class MaterialReservation : TenantEntity
{
    public int InventoryLotId { get; set; }
    public int? ProjectId { get; set; }
    public int? WorkOrderId { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal? ReservedLength { get; set; }
    public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
    public string ReservedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual InventoryLot InventoryLot { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual WorkOrder? WorkOrder { get; set; }
}

public class Certificate : TenantEntity
{
    public string CertificateNumber { get; set; } = string.Empty;
    public string VendorCertificateNumber { get; set; } = string.Empty;
    public string HeatNumber { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public byte[] PdfData { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    // Navigation properties
    public virtual ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();
}