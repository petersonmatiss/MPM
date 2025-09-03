using System.ComponentModel.DataAnnotations;

namespace Mpm.Domain.Entities;

public class AuditEntry : TenantEntity
{
    [Required(ErrorMessage = "Entity type is required")]
    [StringLength(100, ErrorMessage = "Entity type cannot exceed 100 characters")]
    public string EntityType { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Entity ID is required")]
    public int EntityId { get; set; }
    
    [Required(ErrorMessage = "Action is required")]
    [StringLength(50, ErrorMessage = "Action cannot exceed 50 characters")]
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, StatusChange, etc.
    
    [StringLength(100, ErrorMessage = "Field name cannot exceed 100 characters")]
    public string FieldName { get; set; } = string.Empty; // For field-level changes
    
    public string OldValue { get; set; } = string.Empty; // JSON or string representation
    public string NewValue { get; set; } = string.Empty; // JSON or string representation
    
    [Required(ErrorMessage = "User ID is required")]
    [StringLength(100, ErrorMessage = "User ID cannot exceed 100 characters")]
    public string UserId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "User name is required")]
    [StringLength(100, ErrorMessage = "User name cannot exceed 100 characters")]
    public string UserName { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "User role cannot exceed 50 characters")]
    public string UserRole { get; set; } = string.Empty;
    
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    
    [StringLength(100, ErrorMessage = "IP address cannot exceed 100 characters")]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "User agent cannot exceed 200 characters")]
    public string UserAgent { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "Correlation ID cannot exceed 50 characters")]
    public string CorrelationId { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; } = string.Empty; // For status changes or other important actions
    
    [StringLength(1000, ErrorMessage = "Additional context cannot exceed 1000 characters")]
    public string AdditionalContext { get; set; } = string.Empty; // JSON for extra data
}

public static class AuditActions
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string StatusChange = "StatusChange";
    public const string WinnerSelection = "WinnerSelection";
    public const string QuoteSubmission = "QuoteSubmission";
    public const string Approval = "Approval";
    public const string Send = "Send";
    public const string Complete = "Complete";
    public const string Cancel = "Cancel";
    public const string AddLine = "AddLine";
    public const string UpdateLine = "UpdateLine";
    public const string RemoveLine = "RemoveLine";
}

public static class AuditEntityTypes
{
    public const string PurchaseRequest = "PurchaseRequest";
    public const string PurchaseRequestLine = "PurchaseRequestLine";
    public const string PurchaseRequestQuote = "PurchaseRequestQuote";
    public const string PurchaseRequestQuoteItem = "PurchaseRequestQuoteItem";
}