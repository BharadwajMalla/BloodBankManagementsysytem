using BloodBank.Application.Models;

namespace BloodBank.Application.Contracts;

public interface IDonorDonationRepository
{
    Task<IReadOnlyList<Donor>> GetDonorsAsync(CancellationToken cancellationToken);
    Task<Donor?> FindDonorAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> UserEmailExistsAsync(string email, Guid? exceptUserId, CancellationToken cancellationToken);
    Task AddDonorAsync(Donor donor, string passwordHash, CancellationToken cancellationToken);
    Task<bool> UpdateDonorAsync(Donor donor, CancellationToken cancellationToken);
    Task<bool> DeactivateDonorAsync(Guid id, string? actor, CancellationToken cancellationToken);
    Task<IReadOnlyList<Donation>> GetDonationsAsync(IEnumerable<Guid> donorIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<Donation>> GetDonationsForDonorAsync(Guid donorId, CancellationToken cancellationToken);
    Task AddDonationAsync(Donation donation, CancellationToken cancellationToken);
}
