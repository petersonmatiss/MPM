namespace Mpm.Domain.Entities;

public class SheetUsage : TenantEntity
{
    public int SheetId { get; set; }
    public int? ProjectId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public string NestId { get; set; } = string.Empty;
    public DateTime UsageDate { get; set; } = DateTime.UtcNow;
    public string UsedBy { get; set; } = string.Empty;
    public decimal AreaUsed { get; set; } // mm²
    public int UsedLengthMm { get; set; }
    public int UsedWidthMm { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool GeneratedRemnants { get; set; } = false;
    public string RemnantDetails { get; set; } = string.Empty; // JSON array of remnant dimensions

    // Navigation properties
    public virtual Sheet Sheet { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual ManufacturingOrder? ManufacturingOrder { get; set; }
}

public class ProfileUsage : TenantEntity
{
    public int? ProfileId { get; set; }
    public int? ProfileRemnantId { get; set; }
    public int? ProjectId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public DateTime UsageDate { get; set; } = DateTime.UtcNow;
    public string UsedBy { get; set; } = string.Empty;
    public int UsedPieceLength { get; set; } // Length of each piece that was used
    public int PiecesUsed { get; set; } = 1; // Number of pieces used
    public bool RemnantFlag { get; set; } = false;
    public int? RemnantPieceLength { get; set; } // Length of each remnant piece created
    public int? RemnantPiecesCreated { get; set; } // Number of remnant pieces created
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual Profile? Profile { get; set; }
    public virtual ProfileRemnant? ProfileRemnant { get; set; }
    public virtual Project? Project { get; set; }
    public virtual ManufacturingOrder? ManufacturingOrder { get; set; }
}