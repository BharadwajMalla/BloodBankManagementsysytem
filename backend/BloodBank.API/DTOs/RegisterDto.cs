using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&_\-#])[A-Za-z\d@$!%*?&_\-#]{8,}$", 
        ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(BloodBank|Donor|Hospital|Admin)$", ErrorMessage = "Role must be one of: BloodBank, Donor, Hospital.")]
    public string Role { get; set; } = "Donor";

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^(\+91[\-\s]?|0)?[6-9]\d{9}$", 
        ErrorMessage = "Please enter a valid 10-digit Indian mobile number (e.g. 9876543210 or +919876543210).")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Email verification OTP code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification OTP must be a 6-digit code.")]
    public string OtpCode { get; set; } = string.Empty;

    public string? BloodGroup { get; set; } // If Donor (A+, B+, etc.)

    public string? HospitalName { get; set; } // If Hospital

    public string? HospitalRegNumber { get; set; } // If Hospital
}
