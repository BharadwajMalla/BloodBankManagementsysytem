namespace BloodBank.API.Models;

public class LoginAuditRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public DateTime LoginAt { get; set; }
    public bool Succeeded { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}