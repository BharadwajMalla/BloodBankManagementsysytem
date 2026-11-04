using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class BloodInventory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty; // e.g. "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"

    [Range(0, 100000)]
    public int QuantityUnits { get; set; }

    [Required]
    [MaxLength(200)]
    public string BloodBankName { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
