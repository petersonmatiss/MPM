using System.ComponentModel.DataAnnotations;

namespace Mpm.Domain.Entities;

public enum PriceRequestStatus
{
    Draft,
    Sent,
    Responded,
    Closed
}

public class PriceRequest : TenantEntity
{
    [Required(ErrorMessage = "PR number is required")]
    [StringLength(50, ErrorMessage = "PR number cannot exceed 50 characters")]
    public string Number { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? RequiredDate { get; set; }
    
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    public PriceRequestStatus Status { get; set; } = PriceRequestStatus.Draft;
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    public int? ProjectId { get; set; }
    
    // Navigation properties
    public virtual Project? Project { get; set; }
    public virtual ICollection<PriceRequestLine> Lines { get; set; } = new List<PriceRequestLine>();
    public virtual ICollection<PriceRequestSend> Sends { get; set; } = new List<PriceRequestSend>();
}

public class PriceRequestLine : TenantEntity
{
    public int PriceRequestId { get; set; }
    
    public int MaterialId { get; set; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Unit of measure is required")]
    [StringLength(10, ErrorMessage = "Unit of measure cannot exceed 10 characters")]
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual PriceRequest PriceRequest { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
}

public enum PriceRequestSendStatus
{
    Pending,
    Sent,
    Failed,
    Bounced
}

public class PriceRequestSend : TenantEntity
{
    public int PriceRequestId { get; set; }
    
    public int SupplierId { get; set; }
    
    [Required(ErrorMessage = "Recipient email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string RecipientEmail { get; set; } = string.Empty;
    
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public PriceRequestSendStatus Status { get; set; } = PriceRequestSendStatus.Pending;
    
    [StringLength(64, ErrorMessage = "Attachment hash cannot exceed 64 characters")]
    public string AttachmentHash { get; set; } = string.Empty;
    
    [StringLength(255, ErrorMessage = "Subject cannot exceed 255 characters")]
    public string EmailSubject { get; set; } = string.Empty;
    
    [StringLength(2000, ErrorMessage = "Email body cannot exceed 2000 characters")]
    public string EmailBody { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Error message cannot exceed 500 characters")]
    public string ErrorMessage { get; set; } = string.Empty;
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime? LastRetryAt { get; set; }
    
    // Navigation properties
    public virtual PriceRequest PriceRequest { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;
}