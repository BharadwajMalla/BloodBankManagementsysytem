using BloodBank.Application.Dtos;

namespace BloodBank.Application.Contracts;

public interface IDonorDonationService
{
    Task<IReadOnlyList<DonorDetailsDto>> GetDonorsAsync(CancellationToken cancellationToken);
    Task<CreateDonorResponseDto?> CreateDonorAsync(
        CreateDonorCommand command,
        CancellationToken cancellationToken,
        string? actor = null);
    Task<DonorDetailsDto?> UpdateDonorAsync(
        Guid id,
        CreateDonorCommand command,
        CancellationToken cancellationToken,
        string? actor = null);
    Task<bool> DeactivateDonorAsync(Guid id, CancellationToken cancellationToken, string? actor = null);
    Task<DonationCompletionResultDto> CompleteDonationAsync(Guid donorId, CompleteDonationCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyList<DonationHistoryDto>> GetDonationHistoryAsync(Guid donorId, CancellationToken cancellationToken);
}
