namespace Mpm.Domain;

public enum CustomerStatus
{
    Active,
    Archived
}

public enum ExcLevel
{
    EXC1,
    EXC2,
    EXC3,
    EXC4
}

public enum CoatingType
{
    None,
    Galvanized,
    Painted,
    FireProtection,
    Other
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled
}

public enum WorkOrderStatus
{
    Planned,
    InProgress,
    Completed,
    OnHold,
    Cancelled
}

public enum OperationType
{
    Saw,
    Drill,
    Weld,
    Plasma,
    Laser,
    OutsourcePrep,
    OutsourceCoat,
    InternalCoat
}

public enum NCRStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}

public enum DefectType
{
    Dimensional,
    Welding,
    Surface,
    Material,
    Other
}

public enum NCRDisposition
{
    Rework,
    Repair,
    UseAsIs,
    Scrap
}