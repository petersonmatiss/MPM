namespace Mpm.Domain.Entities;

public class Customer : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
    public string Currency { get; set; } = Constants.Currency.EUR;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    // Navigation properties
    public virtual ICollection<CustomerContact> Contacts { get; set; } = new List<CustomerContact>();
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}

public class CustomerContact : TenantEntity
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
}