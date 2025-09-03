namespace Mpm.Domain.Entities;

public class SteelGrade : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty; // EN, ASTM, etc.
    public string Description { get; set; } = string.Empty;
    public decimal DensityKgPerM3 { get; set; } = 7850; // Standard steel density
    public decimal YieldStrengthMPa { get; set; }
    public decimal TensileStrengthMPa { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    public virtual ICollection<Sheet> Sheets { get; set; } = new List<Sheet>();
    public virtual ICollection<Profile> Profiles { get; set; } = new List<Profile>();
}

public class ProfileType : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Beam, Channel, Angle, etc.
    public string Description { get; set; } = string.Empty;
    public decimal StandardWeight { get; set; } // kg/m
    public string DimensionFormat { get; set; } = string.Empty; // e.g., "HxWxT" for beams
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    public virtual ICollection<Profile> Profiles { get; set; } = new List<Profile>();
    public virtual ICollection<PriceRequestLine> PriceRequestLines { get; set; } = new List<PriceRequestLine>();
}