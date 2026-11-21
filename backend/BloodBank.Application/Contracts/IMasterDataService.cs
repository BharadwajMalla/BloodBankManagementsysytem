using BloodBank.Application.Dtos;
using BloodBank.Application.Models;

namespace BloodBank.Application.Contracts;

public interface IMasterDataService
{
    Task<IReadOnlyList<MasterDataEntryDto>> GetAsync(
        MasterDataCategory category,
        string? search,
        CancellationToken cancellationToken);

    Task<MasterDataEntryDto> CreateAsync(
        MasterDataCategory category,
        MasterDataCommand command,
        string actor,
        CancellationToken cancellationToken);

    Task<MasterDataEntryDto> UpdateAsync(
        MasterDataCategory category,
        Guid id,
        MasterDataCommand command,
        string actor,
        CancellationToken cancellationToken);

    Task SetActiveAsync(
        MasterDataCategory category,
        Guid id,
        bool isActive,
        string actor,
        CancellationToken cancellationToken);
}
