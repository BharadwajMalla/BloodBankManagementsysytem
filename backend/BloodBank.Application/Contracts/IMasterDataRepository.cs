using BloodBank.Application.Models;

namespace BloodBank.Application.Contracts;

public interface IMasterDataRepository
{
    Task<IReadOnlyList<MasterDataEntry>> GetAsync(
        MasterDataCategory category,
        string? search,
        CancellationToken cancellationToken);

    Task<MasterDataEntry?> FindAsync(
        MasterDataCategory category,
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> CodeOrEmailExistsAsync(
        MasterDataCategory category,
        string code,
        string? registrationNumber,
        string? email,
        Guid? exceptId,
        CancellationToken cancellationToken);

    Task<MasterDataEntry> CreateAsync(
        MasterDataCategory category,
        MasterDataEntry entry,
        string? passwordHash,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        MasterDataCategory category,
        MasterDataEntry entry,
        CancellationToken cancellationToken);

    Task<bool> SetActiveAsync(
        MasterDataCategory category,
        Guid id,
        bool isActive,
        string actor,
        DateTime updatedAt,
        CancellationToken cancellationToken);
}
