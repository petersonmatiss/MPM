using System.ComponentModel.DataAnnotations;

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

public class PriceRequest : TenantEntity
{
    [Required(ErrorMessage = "Request number is required")]
    [StringLength(50, ErrorMessage = "Request number cannot exceed 50 characters")]
    public string Number { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredByDate { get; set; }
    
    public PriceRequestStatus Status { get; set; } = PriceRequestStatus.Draft;
    
    public string Notes { get; set; } = string.Empty;
    
    // Totals (calculated from lines)
    public decimal TotalQuantity { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal EstimatedTotalValue { get; set; }

    // Navigation properties
    public virtual ICollection<PriceRequestLine> Lines { get; set; } = new List<PriceRequestLine>();
    public virtual ICollection<Supplier> TargetSuppliers { get; set; } = new List<Supplier>();
}

public class PriceRequestLine : TenantEntity
{
    public int PriceRequestId { get; set; }
    
    [Required(ErrorMessage = "Material type is required")]
    public MaterialType MaterialType { get; set; } = MaterialType.Sheet;
    
    [Required(ErrorMessage = "Description is required")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    
    // Steel grade
    public int? SteelGradeId { get; set; }
    
    // Profile specific fields
    public int? ProfileTypeId { get; set; }
    
    // Dimensions in mm (int as per requirements)
    public int? LengthMm { get; set; }
    public int? WidthMm { get; set; }
    public int? ThicknessMm { get; set; }
    public int? HeightMm { get; set; } // For profiles like RHS
    public int? DiameterMm { get; set; } // For round profiles
    
    // Quantity
    [Required(ErrorMessage = "Quantity is required")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Unit of measure is required")]
    [StringLength(10, ErrorMessage = "Unit of measure cannot exceed 10 characters")]
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    
    // For profiles - pieces vs length
    public int? Pieces { get; set; }
    public int? PieceLengthMm { get; set; }
    
    // Surface treatment
    [StringLength(100, ErrorMessage = "Surface treatment cannot exceed 100 characters")]
    public string Surface { get; set; } = string.Empty;
    
    // Estimated values
    public decimal EstimatedUnitPrice { get; set; }
    public decimal EstimatedTotalPrice { get; set; }
    public decimal EstimatedWeight { get; set; }
    
    public string Notes { get; set; } = string.Empty;
    
    public int LineNumber { get; set; } // For ordering

    // Navigation properties
    public virtual PriceRequest PriceRequest { get; set; } = null!;
    public virtual SteelGrade? SteelGrade { get; set; }
    public virtual ProfileType? ProfileType { get; set; }
}

public enum PriceRequestStatus
{
    Draft,
    Sent,
    QuotesReceived,
    Completed,
    Cancelled
}

public enum MaterialType
{
    Sheet,
    Profile
}