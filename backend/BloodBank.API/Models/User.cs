using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty; // "BloodBank", "Donor", "Hospital"

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(10)]
    public string? BloodGroup { get; set; } // For Donors

    [MaxLength(150)]
    public string? HospitalName { get; set; } // For Hospitals

    [MaxLength(50)]
    public string? HospitalRegNumber { get; set; } // For Hospitals

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
