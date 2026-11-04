using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class BloodStock
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty; // "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"

    [Required]
    [MaxLength(50)]
    public string ComponentType { get; set; } = "Whole Blood"; // "Whole Blood", "Red Blood Cells (PRBC)", "Platelets", "Fresh Frozen Plasma (FFP)", "Cryoprecipitate"

    [Range(0, 100000)]
    public int QuantityUnits { get; set; }

    [Required]
    [MaxLength(100)]
    public string BloodBankName { get; set; } = "Blood Bank A";

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
