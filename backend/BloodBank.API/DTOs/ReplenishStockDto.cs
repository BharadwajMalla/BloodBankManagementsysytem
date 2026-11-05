using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class ReplenishStockDto
{
    [Required(ErrorMessage = "Blood group is required.")]
    public string BloodGroup { get; set; } = string.Empty;

    [Required(ErrorMessage = "Component type is required.")]
    public string ComponentType { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Quantity must be between 1 and 100,000 units.")]
    public int QuantityUnits { get; set; } = 1;
}
