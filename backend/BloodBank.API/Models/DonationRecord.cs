using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class DonationRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DonorId { get; set; }

    [Required]
    [MaxLength(150)]
    public string DonorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty;

    public int UnitsDonated { get; set; } = 1;

    public DateTime DonationDate { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? CampName { get; set; } // If donated at a specific camp

    [MaxLength(50)]
    public string BloodBankName { get; set; } = "Blood Bank A";
}
