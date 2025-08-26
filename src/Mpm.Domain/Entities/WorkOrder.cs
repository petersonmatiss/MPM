namespace Mpm.Domain.Entities;

public class WorkOrder : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public OperationType OperationType { get; set; }
    public DateTime? DueDate { get; set; }
    public int Priority { get; set; } = 1;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Planned;
    public string Description { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<WorkOrderOperation> Operations { get; set; } = new List<WorkOrderOperation>();
    public virtual ICollection<MaterialReservation> MaterialReservations { get; set; } = new List<MaterialReservation>();
}

public class WorkOrderOperation : TenantEntity
{
    public int WorkOrderId { get; set; }
    public string OperatorBadgeId { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int? QuantityCompleted { get; set; }
    public decimal? ActualHours { get; set; }

    // Navigation properties
    public virtual WorkOrder WorkOrder { get; set; } = null!;
}

public class NonConformanceReport : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public int? WorkOrderId { get; set; }
    public DefectType DefectType { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NCRDisposition Disposition { get; set; }
    public NCRStatus Status { get; set; } = NCRStatus.Open;
    public string RootCause { get; set; } = string.Empty;
    public string CorrectiveAction { get; set; } = string.Empty;
    public string PreventiveAction { get; set; } = string.Empty;
    public DateTime DiscoveryDate { get; set; } = DateTime.UtcNow;
    public string DiscoveredBy { get; set; } = string.Empty;
    public DateTime? ClosureDate { get; set; }
    public string ClosedBy { get; set; } = string.Empty;

    // For EXC4 - dual sign requirement
    public string QAApprovalBy { get; set; } = string.Empty;
    public DateTime? QAApprovalDate { get; set; }
    public string PMApprovalBy { get; set; } = string.Empty;
    public DateTime? PMApprovalDate { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual WorkOrder? WorkOrder { get; set; }
    public virtual ICollection<NCRPhoto> Photos { get; set; } = new List<NCRPhoto>();
}

public class NCRPhoto : TenantEntity
{
    public int NonConformanceReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string Caption { get; set; } = string.Empty;

    // Navigation properties
    public virtual NonConformanceReport NonConformanceReport { get; set; } = null!;
}