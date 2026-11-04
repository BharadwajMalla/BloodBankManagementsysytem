using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class BloodInventoryCreateDto
{
    [Required(ErrorMessage = "Blood group is required.")]
    [RegularExpression("^(A\\+|A\\-|B\\+|B\\-|AB\\+|AB\\-|O\\+|O\\-)$", ErrorMessage = "Blood group must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.")]
    public string BloodGroup { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quantity in units is required.")]
    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000 units.")]
    public int QuantityUnits { get; set; }

    [Required(ErrorMessage = "Blood bank name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Blood bank name must be between 2 and 200 characters.")]
    public string BloodBankName { get; set; } = string.Empty;
}
