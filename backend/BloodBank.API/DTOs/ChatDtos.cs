using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public sealed class ChatRequestDto
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(8)]
    public List<ChatTurnDto> History { get; set; } = [];
}

public sealed class ChatTurnDto
{
    [Required]
    [RegularExpression("^(user|assistant)$")]
    public string Role { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public sealed record ChatResponseDto(string Answer, DateTime DataAsOfUtc);
