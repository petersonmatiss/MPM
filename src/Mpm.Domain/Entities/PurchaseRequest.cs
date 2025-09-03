using System.ComponentModel.DataAnnotations;

namespace Mpm.Domain.Entities;

public class PurchaseRequest : TenantEntity
{
    [Required(ErrorMessage = "PR number is required")]
    [StringLength(50, ErrorMessage = "Number cannot exceed 50 characters")]
    public string Number { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;
    
    public int? ProjectId { get; set; }
    
    [Required(ErrorMessage = "Requested by is required")]
    [StringLength(100, ErrorMessage = "Requested by cannot exceed 100 characters")]
    public string RequestedBy { get; set; } = string.Empty;
    
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredDate { get; set; }
    
    [Required]
    public PRStatus Status { get; set; } = PRStatus.Draft;
    
    [StringLength(100, ErrorMessage = "Approved by cannot exceed 100 characters")]
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime? ApprovedDate { get; set; }
    
    [StringLength(100, ErrorMessage = "Sent by cannot exceed 100 characters")]
    public string SentBy { get; set; } = string.Empty;
    public DateTime? SentDate { get; set; }
    
    [StringLength(100, ErrorMessage = "Completed by cannot exceed 100 characters")]
    public string CompletedBy { get; set; } = string.Empty;
    public DateTime? CompletedDate { get; set; }
    
    [StringLength(100, ErrorMessage = "Canceled by cannot exceed 100 characters")]
    public string CanceledBy { get; set; } = string.Empty;
    public DateTime? CanceledDate { get; set; }
    
    [StringLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters")]
    public string CancellationReason { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    // Winner selection tracking
    public int? WinnerSupplierId { get; set; }
    public string WinnerSelectedBy { get; set; } = string.Empty;
    public DateTime? WinnerSelectedDate { get; set; }
    public string WinnerSelectionReason { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual Project? Project { get; set; }
    public virtual Supplier? WinnerSupplier { get; set; }
    public virtual ICollection<PurchaseRequestLine> Lines { get; set; } = new List<PurchaseRequestLine>();
    public virtual ICollection<PurchaseRequestQuote> Quotes { get; set; } = new List<PurchaseRequestQuote>();
}

public class PurchaseRequestLine : TenantEntity
{
    public int PurchaseRequestId { get; set; }
    public int MaterialId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Unit of measure is required")]
    [StringLength(20, ErrorMessage = "Unit of measure cannot exceed 20 characters")]
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    
    [StringLength(100, ErrorMessage = "Profile type cannot exceed 100 characters")]
    public string ProfileType { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Specifications cannot exceed 500 characters")]
    public string Specifications { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
    
    public DateTime? RequiredDate { get; set; }
    
    // Navigation properties
    public virtual PurchaseRequest PurchaseRequest { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
    public virtual ICollection<PurchaseRequestQuoteItem> QuoteItems { get; set; } = new List<PurchaseRequestQuoteItem>();
}

public class PurchaseRequestQuote : TenantEntity
{
    public int PurchaseRequestId { get; set; }
    public int SupplierId { get; set; }
    
    [Required(ErrorMessage = "Quote reference is required")]
    [StringLength(100, ErrorMessage = "Quote reference cannot exceed 100 characters")]
    public string QuoteReference { get; set; } = string.Empty;
    
    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }
    
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    [StringLength(100, ErrorMessage = "Payment terms cannot exceed 100 characters")]
    public string PaymentTerms { get; set; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "Delivery terms cannot exceed 100 characters")]
    public string DeliveryTerms { get; set; } = string.Empty;
    
    [Range(0, 365, ErrorMessage = "Delivery days must be between 0 and 365")]
    public int DeliveryDays { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    public bool IsSelected { get; set; } = false;
    public string SelectedBy { get; set; } = string.Empty;
    public DateTime? SelectedDate { get; set; }
    public string SelectionReason { get; set; } = string.Empty;
    
    public decimal TotalAmount { get; set; }
    
    // Navigation properties
    public virtual PurchaseRequest PurchaseRequest { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<PurchaseRequestQuoteItem> Items { get; set; } = new List<PurchaseRequestQuoteItem>();
}

public class PurchaseRequestQuoteItem : TenantEntity
{
    public int PurchaseRequestQuoteId { get; set; }
    public int PurchaseRequestLineId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative")]
    public decimal UnitPrice { get; set; }
    
    [Range(0, 100, ErrorMessage = "Discount percent must be between 0 and 100")]
    public decimal DiscountPercent { get; set; } = 0;
    
    [StringLength(20, ErrorMessage = "Tax code cannot exceed 20 characters")]
    public string TaxCode { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
    
    // Calculated field
    public decimal LineTotal => Quantity * UnitPrice * (1 - DiscountPercent / 100);
    
    // Navigation properties
    public virtual PurchaseRequestQuote Quote { get; set; } = null!;
    public virtual PurchaseRequestLine RequestLine { get; set; } = null!;
}