namespace BloodBank.API.DTOs;

/// <summary>One recorded login attempt returned by the audit report.</summary>
public sealed record LoginAuditResponse(
    Guid Id,
    Guid? UserId,
    string Email,
    string? FullName,
    string? Role,
    DateTimeOffset LoginAt,
    bool Succeeded,
    string? IpAddress,
    string? UserAgent);