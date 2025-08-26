using System.ComponentModel.DataAnnotations;

namespace Mpm.Domain.Entities;

public class Invoice : TenantEntity
{
    [Required(ErrorMessage = "Invoice number is required")]
    [StringLength(50, ErrorMessage = "Invoice number cannot exceed 50 characters")]
    public string Number { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Supplier is required")]
    public int SupplierId { get; set; }
    
    [Required(ErrorMessage = "Invoice date is required")]
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? DueDate { get; set; }
    
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, ErrorMessage = "Currency must be a 3-letter ISO 4217 code")]
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    [Range(0, double.MaxValue, ErrorMessage = "SubTotal must be non-negative")]
    public decimal SubTotal { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "TaxAmount must be non-negative")]
    public decimal TaxAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be non-negative")]
    public decimal TotalAmount { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    public bool IsReceived { get; set; } = false;
    public DateTime? ReceivedDate { get; set; }
    
    [StringLength(100, ErrorMessage = "ReceivedBy cannot exceed 100 characters")]
    public string ReceivedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}

public class InvoiceLine : TenantEntity
{
    [Required(ErrorMessage = "Invoice is required")]
    public int InvoiceId { get; set; }
    
    [Required(ErrorMessage = "Description is required")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "ItemType cannot exceed 50 characters")]
    public string ItemType { get; set; } = string.Empty; // "Sheet" or "Profile"
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Unit of measure is required")]
    [StringLength(10, ErrorMessage = "Unit of measure cannot exceed 10 characters")]
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    
    [Required(ErrorMessage = "Unit price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal UnitPrice { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Total price must be non-negative")]
    public decimal TotalPrice { get; set; }
    
    [StringLength(20, ErrorMessage = "Tax code cannot exceed 20 characters")]
    public string TaxCode { get; set; } = string.Empty;
    
    // Material specifications
    [StringLength(50, ErrorMessage = "Grade cannot exceed 50 characters")]
    public string Grade { get; set; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "Dimension cannot exceed 100 characters")]
    public string Dimension { get; set; } = string.Empty;
    
    [Range(1, int.MaxValue, ErrorMessage = "Length must be positive")]
    public int? LengthMm { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Width must be positive")]
    public int? WidthMm { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Thickness must be positive")]
    public int? ThicknessMm { get; set; }
    
    [StringLength(50, ErrorMessage = "Profile type cannot exceed 50 characters")]
    public string ProfileType { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "Heat number cannot exceed 50 characters")]
    public string HeatNumber { get; set; } = string.Empty;

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
}