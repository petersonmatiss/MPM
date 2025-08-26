namespace Mpm.Domain.Entities;

public class Material : TenantEntity
{
    public string Grade { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public string ProfileType { get; set; } = string.Empty;
    public decimal UnitWeight { get; set; }
    public string Surface { get; set; } = string.Empty;
    public string SupplierPartNumber { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<BomItem> BomItems { get; set; } = new List<BomItem>();
    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
    public virtual ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();
}

public class Supplier : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
    public string Currency { get; set; } = Constants.Currency.EUR;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

public class PurchaseOrder : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int? ProjectId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveryDate { get; set; }
    public string Incoterms { get; set; } = string.Empty;
    public string Currency { get; set; } = Constants.Currency.EUR;
    public string Notes { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; } = false;

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public virtual ICollection<GoodsReceiptNote> GoodsReceiptNotes { get; set; } = new List<GoodsReceiptNote>();
}

public class PurchaseOrderLine : TenantEntity
{
    public int PurchaseOrderId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public string TaxCode { get; set; } = string.Empty;
    public string ProfileType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
    public virtual ICollection<GoodsReceiptNoteLine> GoodsReceiptNoteLines { get; set; } = new List<GoodsReceiptNoteLine>();
}