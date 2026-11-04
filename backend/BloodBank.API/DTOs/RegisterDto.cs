using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(BloodBank|Donor|Hospital|Admin)$", ErrorMessage = "Role must be one of: BloodBank, Donor, Hospital.")]
    public string Role { get; set; } = "Donor";

    public string? Phone { get; set; }

    public string? BloodGroup { get; set; } // If Donor (A+, B+, etc.)

    public string? HospitalName { get; set; } // If Hospital

    public string? HospitalRegNumber { get; set; } // If Hospital
}
