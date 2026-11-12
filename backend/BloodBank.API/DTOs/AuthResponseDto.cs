namespace BloodBank.API.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = default!;
    // public DateTime ExpiresAt { get; set; }
}
