using BloodBank.API.DTOs;

namespace BloodBank.API.Services;

public interface IInventoryChatService
{
    Task<ChatResponseDto?> AskAsync(ChatRequestDto request, CancellationToken cancellationToken);
}
