using System.ComponentModel.DataAnnotations;

namespace Mpm.Domain.Entities;

public class PurchaseRequest : TenantEntity
{
    [Required(ErrorMessage = "Request number is required")]
    [StringLength(50, ErrorMessage = "Request number cannot exceed 50 characters")]
    public string Number { get; set; } = string.Empty;
    
    public int? ProjectId { get; set; }
    
    [Required(ErrorMessage = "Request date is required")]
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? RequiredDate { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    public bool IsCompleted { get; set; } = false;
    
    public DateTime? CompletedDate { get; set; }
    
    // Navigation properties
    public virtual Project? Project { get; set; }
    public virtual ICollection<PurchaseRequestLine> Lines { get; set; } = new List<PurchaseRequestLine>();
}

public class PurchaseRequestLine : TenantEntity
{
    public int PurchaseRequestId { get; set; }
    
    public int MaterialId { get; set; }
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Unit of measure is required")]
    [StringLength(20, ErrorMessage = "Unit of measure cannot exceed 20 characters")]
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    
    public DateTime? RequiredDate { get; set; }
    
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
    
    // Winner selection
    public int? WinnerSupplierId { get; set; }
    public int? WinnerQuoteLineId { get; set; }
    public DateTime? WinnerSelectedDate { get; set; }
    public string WinnerSelectedBy { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual PurchaseRequest PurchaseRequest { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
    public virtual Supplier? WinnerSupplier { get; set; }
    public virtual SupplierQuoteLine? WinnerQuoteLine { get; set; }
    public virtual ICollection<SupplierQuoteLine> SupplierQuotes { get; set; } = new List<SupplierQuoteLine>();
}

public class SupplierQuote : TenantEntity
{
    public int PurchaseRequestId { get; set; }
    public int SupplierId { get; set; }
    
    [Required(ErrorMessage = "Quote number is required")]
    [StringLength(50, ErrorMessage = "Quote number cannot exceed 50 characters")]
    public string QuoteNumber { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Quote date is required")]
    public DateTime QuoteDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ValidUntil { get; set; }
    
    [StringLength(3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    [StringLength(100, ErrorMessage = "Payment terms cannot exceed 100 characters")]
    public string PaymentTerms { get; set; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "Delivery terms cannot exceed 100 characters")]
    public string DeliveryTerms { get; set; } = string.Empty;
    
    public int LeadTimeDays { get; set; } = 0;
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    public decimal TotalAmount { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual PurchaseRequest PurchaseRequest { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<SupplierQuoteLine> Lines { get; set; } = new List<SupplierQuoteLine>();
}

public class SupplierQuoteLine : TenantEntity
{
    public int SupplierQuoteId { get; set; }
    public int PurchaseRequestLineId { get; set; }
    
    [Required(ErrorMessage = "Unit price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be greater than or equal to 0")]
    public decimal UnitPrice { get; set; }
    
    [Range(0, 100, ErrorMessage = "Discount percent must be between 0 and 100")]
    public decimal DiscountPercent { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Total price must be greater than or equal to 0")]
    public decimal TotalPrice { get; set; }
    
    public int LeadTimeDays { get; set; } = 0;
    
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
    
    public bool IsAvailable { get; set; } = true;
    
    // Navigation properties
    public virtual SupplierQuote SupplierQuote { get; set; } = null!;
    public virtual PurchaseRequestLine PurchaseRequestLine { get; set; } = null!;
}