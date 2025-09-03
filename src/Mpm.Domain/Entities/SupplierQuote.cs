using System.ComponentModel.DataAnnotations;

namespace Mpm.Domain.Entities;

public class SupplierQuote : TenantEntity
{
    public int PurchaseOrderLineId { get; set; }
    public int SupplierId { get; set; }
    
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    [Required(ErrorMessage = "Validity date is required")]
    public DateTime ValidityDate { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Lead time must be at least 1 day")]
    public int? LeadTimeDays { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;
}