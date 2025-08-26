namespace Mpm.Domain.Entities;

public class ManufacturingOrder : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Planned;
    public string Instructions { get; set; } = string.Empty;
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<MoDrawing> Drawings { get; set; } = new List<MoDrawing>();
    public virtual ICollection<TimeLog> TimeLogs { get; set; } = new List<TimeLog>();
    public virtual ICollection<SheetUsage> SheetUsages { get; set; } = new List<SheetUsage>();
    public virtual ICollection<ProfileUsage> ProfileUsages { get; set; } = new List<ProfileUsage>();
}

public class MoDrawing : TenantEntity
{
    public int ManufacturingOrderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string BlobUri { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ManufacturingOrder ManufacturingOrder { get; set; } = null!;
}

public class TimeLog : TenantEntity
{
    public int? ProjectId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public string WorkerBadgeId { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public DateTime ClockInTime { get; set; } = DateTime.UtcNow;
    public DateTime? ClockOutTime { get; set; }
    public decimal? HoursWorked { get; set; }
    public OperationType OperationType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool AutoClockOut { get; set; } = false; // True if auto-clocked out at 17:01

    // Navigation properties
    public virtual Project? Project { get; set; }
    public virtual ManufacturingOrder? ManufacturingOrder { get; set; }
}

public class Notification : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string RecipientUserId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }
    public bool SmsSent { get; set; } = false;
    public DateTime? SmsSentAt { get; set; }
    public bool TeamsSent { get; set; } = false;
    public DateTime? TeamsSentAt { get; set; }
    public int? RelatedProjectId { get; set; }
    public int? RelatedManufacturingOrderId { get; set; }
    public string ExternalReference { get; set; } = string.Empty;

    // Navigation properties
    public virtual Project? RelatedProject { get; set; }
    public virtual ManufacturingOrder? RelatedManufacturingOrder { get; set; }
}