using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class VerifyOtpDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "6-digit OTP code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
    public string OtpCode { get; set; } = string.Empty;
}
