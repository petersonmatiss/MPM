namespace Mpm.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public abstract class TenantEntity : BaseEntity
{
    public string TenantId { get; set; } = string.Empty;
}