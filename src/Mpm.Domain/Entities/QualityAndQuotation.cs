namespace Mpm.Domain.Entities;

public class DeclarationOfPerformance : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string IntendedUse { get; set; } = string.Empty;
    public string EssentialCharacteristics { get; set; } = string.Empty;
    public string DeclaredPerformance { get; set; } = string.Empty;
    public string AVCPSystem { get; set; } = string.Empty;
    public string NotifiedBodyId { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public string IssuedBy { get; set; } = string.Empty;
    public byte[] PdfData { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string HashChain { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;

    // For EXC4 - dual sign requirement
    public string QASignedBy { get; set; } = string.Empty;
    public DateTime? QASignedDate { get; set; }
    public string PMSignedBy { get; set; } = string.Empty;
    public DateTime? PMSignedDate { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<DoPMaterial> Materials { get; set; } = new List<DoPMaterial>();
    public virtual ICollection<DoPHeat> Heats { get; set; } = new List<DoPHeat>();
}

public class DoPMaterial : TenantEntity
{
    public int DeclarationOfPerformanceId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;

    // Navigation properties
    public virtual DeclarationOfPerformance DeclarationOfPerformance { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
}

public class DoPHeat : TenantEntity
{
    public int DeclarationOfPerformanceId { get; set; }
    public string HeatNumber { get; set; } = string.Empty;
    public int? CertificateId { get; set; }

    // Navigation properties
    public virtual DeclarationOfPerformance DeclarationOfPerformance { get; set; } = null!;
    public virtual Certificate? Certificate { get; set; }
}

public class Quotation : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalWeight { get; set; }
    public decimal ComplexityFactor { get; set; } = 1.0m;
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal TotalCost { get; set; }
    public decimal SellingPrice { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsAccepted { get; set; } = false;
    public DateTime? AcceptedDate { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual ICollection<QuotationLine> Lines { get; set; } = new List<QuotationLine>();
}

public class QuotationLine : TenantEntity
{
    public int QuotationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public OperationType OperationType { get; set; }

    // Navigation properties
    public virtual Quotation Quotation { get; set; } = null!;
}

public class PriceRequest : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PriceRequestStatus Status { get; set; } = PriceRequestStatus.Draft;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedDate { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<PriceRequestLine> Lines { get; set; } = new List<PriceRequestLine>();
}

public class PriceRequestLine : TenantEntity
{
    public int PriceRequestId { get; set; }
    public MaterialType MaterialType { get; set; }
    public string Dimensions { get; set; } = string.Empty;
    public decimal TotalLength { get; set; } // for profiles, in mm
    public int PieceCount { get; set; } // for sheets or profile pieces
    public string SteelGrade { get; set; } = string.Empty;
    public string ProfileType { get; set; } = string.Empty; // mandatory for profiles
    public string Notes { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual PriceRequest PriceRequest { get; set; } = null!;
}