namespace Mpm.Domain.Entities;

public class Invoice : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string Currency { get; set; } = Constants.Currency.EUR;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsReceived { get; set; } = false;
    public DateTime? ReceivedDate { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}

public class InvoiceLine : TenantEntity
{
    public int InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty; // "Sheet" or "Profile"
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string TaxCode { get; set; } = string.Empty;
    
    // Material specifications
    public string Grade { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public int? LengthMm { get; set; }
    public int? WidthMm { get; set; }
    public int? ThicknessMm { get; set; }
    public string ProfileType { get; set; } = string.Empty;
    public string HeatNumber { get; set; } = string.Empty;

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
}