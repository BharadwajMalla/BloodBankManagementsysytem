using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class SendOtpDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
