namespace Mpm.Services.DTOs;

public class ProfileUsageRequest
{
    public int? ProjectId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public string UsedBy { get; set; } = string.Empty;
    public int UsedLengthMm { get; set; }
    public int PiecesUsed { get; set; } = 1;
    public int? RemnantLengthMm { get; set; }
    public string Notes { get; set; } = string.Empty;
}