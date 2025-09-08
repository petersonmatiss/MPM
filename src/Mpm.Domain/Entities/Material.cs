using System.ComponentModel.DataAnnotations;
using Mpm.Domain;

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
    [Required(ErrorMessage = "Supplier name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "VAT number is required")]
    [RegularExpression(@"^[A-Z]{2}[A-Z0-9]{2,12}$", ErrorMessage = "VAT number must be in valid EU format (e.g., LV12345678901)")]
    public string VatNumber { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "Registration number cannot exceed 50 characters")]
    public string RegistrationNumber { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "Payment terms cannot exceed 100 characters")]
    public string PaymentTerms { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
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
    public DateTime? SentDate { get; set; }
    public string Incoterms { get; set; } = string.Empty;
    public string Currency { get; set; } = Constants.Currency.EUR;
    public string Notes { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; } = false;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public virtual ICollection<GoodsReceiptNote> GoodsReceiptNotes { get; set; } = new List<GoodsReceiptNote>();
    public virtual ICollection<PurchaseOrderDocument> Documents { get; set; } = new List<PurchaseOrderDocument>();
    public virtual ICollection<PurchaseOrderCommunication> Communications { get; set; } = new List<PurchaseOrderCommunication>();
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

public class PurchaseOrderDocument : TenantEntity
{
    public int PurchaseOrderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty; // e.g., "Contract", "Invoice", "Specification"
    public string Description { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}

public class PurchaseOrderCommunication : TenantEntity
{
    public int PurchaseOrderId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CommunicationType { get; set; } = string.Empty; // e.g., "Email", "Phone", "Meeting", "Note"
    public string Direction { get; set; } = string.Empty; // "Inbound", "Outbound"
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public DateTime CommunicationDate { get; set; } = DateTime.UtcNow;
    public string RecordedBy { get; set; } = string.Empty;
    public bool IsImportant { get; set; } = false;

    // Navigation properties
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}