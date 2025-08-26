namespace Mpm.Domain.Entities;

public class Sheet : TenantEntity
{
    public string SheetId { get; set; } = string.Empty; // Unique identifier for the sheet
    public int? InvoiceLineId { get; set; }
    public int? ProjectId { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int LengthMm { get; set; }
    public int WidthMm { get; set; }
    public int ThicknessMm { get; set; }
    public decimal Weight { get; set; }
    public string HeatNumber { get; set; } = string.Empty;
    public int? CertificateId { get; set; }
    public DateTime ArrivalDate { get; set; } = DateTime.UtcNow;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsReserved { get; set; } = false;
    public bool IsUsed { get; set; } = false;

    // Navigation properties
    public virtual InvoiceLine? InvoiceLine { get; set; }
    public virtual Project? Project { get; set; }
    public virtual Certificate? Certificate { get; set; }
    public virtual ICollection<SheetUsage> Usages { get; set; } = new List<SheetUsage>();
}

public class Profile : TenantEntity
{
    public string LotId { get; set; } = string.Empty; // Format: One uppercase letter + sequential number (A15)
    public int? InvoiceLineId { get; set; }
    public int? ProjectId { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string ProfileType { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public int LengthMm { get; set; }
    public decimal Weight { get; set; }
    public string HeatNumber { get; set; } = string.Empty;
    public int? CertificateId { get; set; }
    public DateTime ArrivalDate { get; set; } = DateTime.UtcNow;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsReserved { get; set; } = false;
    public int AvailableLengthMm { get; set; } // Current available length after cuts

    // Navigation properties
    public virtual InvoiceLine? InvoiceLine { get; set; }
    public virtual Project? Project { get; set; }
    public virtual Certificate? Certificate { get; set; }
    public virtual ICollection<ProfileUsage> Usages { get; set; } = new List<ProfileUsage>();
    public virtual ICollection<ProfileRemnant> Remnants { get; set; } = new List<ProfileRemnant>();
}

public class ProfileRemnant : TenantEntity
{
    public int ProfileId { get; set; }
    public string RemnantId { get; set; } = string.Empty; // Generated ID for remnant
    public int LengthMm { get; set; }
    public decimal Weight { get; set; }
    public bool IsUsable { get; set; } = true;
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual Profile Profile { get; set; } = null!;
    public virtual ICollection<ProfileUsage> Usages { get; set; } = new List<ProfileUsage>();
}