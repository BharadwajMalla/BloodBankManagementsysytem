using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class DonationCamp
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string CampName { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Location { get; set; } = string.Empty;

    public DateTime CampDate { get; set; } = DateTime.UtcNow.AddDays(7);

    [Range(1, 10000)]
    public int TargetUnits { get; set; } = 50;

    public int CollectedUnits { get; set; } = 0;

    [MaxLength(100)]
    public string OrganizedBy { get; set; } = "Blood Bank A";

    [MaxLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Status { get; set; } = "Scheduled"; // Scheduled, Ongoing, Completed, Cancelled

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
