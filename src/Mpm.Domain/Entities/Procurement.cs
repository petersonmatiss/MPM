using System.ComponentModel.DataAnnotations;

namespace Mpm.Domain.Entities;

public class PriceRequest : TenantEntity
{
    [Required(ErrorMessage = "Price request number is required")]
    [StringLength(50, ErrorMessage = "Number cannot exceed 50 characters")]
    public string Number { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    
    public PriceRequestStatus Status { get; set; } = PriceRequestStatus.Draft;
    
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? SentDate { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "Requested by name cannot exceed 100 characters")]
    public string RequestedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<PriceRequestLine> Lines { get; set; } = new List<PriceRequestLine>();
    public virtual ICollection<PriceRequestSupplier> Suppliers { get; set; } = new List<PriceRequestSupplier>();
}

public class PriceRequestLine : TenantEntity
{
    public int PriceRequestId { get; set; }
    
    public MaterialType MaterialType { get; set; }
    
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string Description { get; set; } = string.Empty;
    
    // Dimensions (all in millimeters as integers, following existing pattern)
    public int? LengthMm { get; set; }
    public int? WidthMm { get; set; }  // For sheets
    public int? ThicknessMm { get; set; }
    
    [StringLength(100, ErrorMessage = "Dimension cannot exceed 100 characters")]
    public string Dimension { get; set; } = string.Empty;  // For profiles (e.g., "200x200x15")
    
    // Quantity requirements
    public decimal TotalLength { get; set; }  // For profiles - total length needed
    public int Pieces { get; set; }  // Number of pieces needed
    
    public int? SteelGradeId { get; set; }
    
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual PriceRequest PriceRequest { get; set; } = null!;
    public virtual SteelGrade? SteelGrade { get; set; }
    public virtual ICollection<PriceRequestQuote> Quotes { get; set; } = new List<PriceRequestQuote>();
}

public class PriceRequestSupplier : TenantEntity
{
    public int PriceRequestId { get; set; }
    public int SupplierId { get; set; }
    
    public DateTime? InvitedDate { get; set; }
    public DateTime? ResponseDate { get; set; }
    
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual PriceRequest PriceRequest { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;
}

public class PriceRequestQuote : TenantEntity
{
    public int PriceRequestLineId { get; set; }
    public int SupplierId { get; set; }
    
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    public int LeadTimeDays { get; set; }
    
    public DateTime QuoteDate { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }
    
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual PriceRequestLine PriceRequestLine { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;
}