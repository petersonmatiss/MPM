using System.ComponentModel.DataAnnotations;
using Mpm.Domain;
using Mpm.Domain.Entities;

namespace Mpm.Services.DTOs;

public class CreatePriceRequestDto
{
    [Required(ErrorMessage = "Request number is required")]
    [StringLength(50, ErrorMessage = "Request number cannot exceed 50 characters")]
    public string Number { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = Constants.Currency.EUR;
    
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredByDate { get; set; }
    
    public string Notes { get; set; } = string.Empty;
    
    public List<CreatePriceRequestLineDto> Lines { get; set; } = new();
}

public class UpdatePriceRequestDto : CreatePriceRequestDto
{
    public int Id { get; set; }
    public PriceRequestStatus Status { get; set; } = PriceRequestStatus.Draft;
    
    // Calculated totals
    public decimal TotalQuantity { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal EstimatedTotalValue { get; set; }
}

public class CreatePriceRequestLineDto
{
    [Required(ErrorMessage = "Material type is required")]
    public MaterialType MaterialType { get; set; } = MaterialType.Sheet;
    
    [Required(ErrorMessage = "Description is required")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    
    // Steel grade
    public int? SteelGradeId { get; set; }
    
    // Profile specific fields
    public int? ProfileTypeId { get; set; }
    
    // Dimensions in mm (int as per requirements)
    public int? LengthMm { get; set; }
    public int? WidthMm { get; set; }
    public int? ThicknessMm { get; set; }
    public int? HeightMm { get; set; } // For profiles like RHS
    public int? DiameterMm { get; set; } // For round profiles
    
    // Quantity
    [Required(ErrorMessage = "Quantity is required")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Unit of measure is required")]
    [StringLength(10, ErrorMessage = "Unit of measure cannot exceed 10 characters")]
    public string UnitOfMeasure { get; set; } = Constants.UnitOfMeasure.Kilogram;
    
    // For profiles - pieces vs length
    public int? Pieces { get; set; }
    public int? PieceLengthMm { get; set; }
    
    // Surface treatment
    [StringLength(100, ErrorMessage = "Surface treatment cannot exceed 100 characters")]
    public string Surface { get; set; } = string.Empty;
    
    // Estimated values
    public decimal EstimatedUnitPrice { get; set; }
    public decimal EstimatedTotalPrice { get; set; }
    public decimal EstimatedWeight { get; set; }
    
    public string Notes { get; set; } = string.Empty;
    
    public int LineNumber { get; set; } // For ordering
}

public class UpdatePriceRequestLineDto : CreatePriceRequestLineDto
{
    public int Id { get; set; }
    public int PriceRequestId { get; set; }
}

public class PriceRequestLineValidationDto : CreatePriceRequestLineDto
{
    // Used for client-side validation without requiring entity IDs
    public string SteelGradeCode { get; set; } = string.Empty;
    public string ProfileTypeCode { get; set; } = string.Empty;
}