namespace Mpm.Domain.Entities;

public class Project : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public ExcLevel ExcLevel { get; set; } = ExcLevel.EXC1;
    public bool En1090Applicable { get; set; } = true;
    public DateTime? DesignDueDate { get; set; }
    public DateTime? FabricationDueDate { get; set; }
    public DateTime? DeliveryDueDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public string SiteAddress { get; set; } = string.Empty;
    public CoatingType CoatingType { get; set; } = CoatingType.None;
    public string CoatingNotes { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<BillOfMaterial> BillOfMaterials { get; set; } = new List<BillOfMaterial>();
    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    public virtual ICollection<DeclarationOfPerformance> DeclarationsOfPerformance { get; set; } = new List<DeclarationOfPerformance>();
}

public class BillOfMaterial : TenantEntity
{
    public int ProjectId { get; set; }
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<BomItem> Items { get; set; } = new List<BomItem>();
}

public class BomItem : TenantEntity
{
    public int BillOfMaterialId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    public decimal Weight { get; set; }
    public decimal? Length { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual BillOfMaterial BillOfMaterial { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
}