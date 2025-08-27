namespace Mpm.Services.DTOs;

public class ProfileUsageRequest
{
    public int? ProjectId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public string UsedBy { get; set; } = string.Empty;
    public int UsedPieceLength { get; set; } // Length of each piece being used
    public int PiecesUsed { get; set; } = 1; // Number of pieces being used
    public int? RemnantPieceLength { get; set; } // Length of each remnant piece created (if any)
    public int? RemnantPiecesCreated { get; set; } // Number of remnant pieces created (if any)
    public string Notes { get; set; } = string.Empty;
}

public class RemnantUsageRequest
{
    public int? ProjectId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public string UsedBy { get; set; } = string.Empty;
    public int UsedPieceLength { get; set; } // Length of each remnant piece being used
    public int PiecesUsed { get; set; } = 1; // Number of remnant pieces being used
    public int? NewRemnantPieceLength { get; set; } // Length of each new remnant piece created (if any)
    public int? NewRemnantPiecesCreated { get; set; } // Number of new remnant pieces created (if any)
    public string Notes { get; set; } = string.Empty;
}