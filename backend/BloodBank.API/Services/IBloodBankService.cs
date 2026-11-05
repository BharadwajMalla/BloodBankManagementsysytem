using BloodBank.API.DTOs;
using BloodBank.API.Models;

namespace BloodBank.API.Services;
public interface IBloodBankService
{
    Task<IEnumerable<BloodStockDto>> GetStockAsync(string? group, string? component);
    Task<BloodStockDto?> AddStockAsync(AddStockDto dto);
    Task<BloodStockDto> ReplenishStockAsync(ReplenishStockDto dto);
    Task ClearAllDataAsync();
    Task<bool> BagExistsAsync(string number);
    Task<IEnumerable<BloodBagDto>> GetBagsAsync(string? component);
    Task<IEnumerable<DonationCamp>> GetCampsAsync();
    Task<DonationCamp?> GetCampAsync(Guid id);
    Task<DonationCamp> CreateCampAsync(CreateCampDto dto);
    Task<DonationCamp?> UpdateCampAsync(Guid id, CreateCampDto dto);
    Task<bool> DeleteCampAsync(Guid id);
    Task<IEnumerable<DonorDetailsDto>> GetDonorsAsync();
    Task<CreateDonorResponseDto?> CreateDonorAsync(CreateDonorDto dto);
    Task<DonationCompletionResultDto> CompleteDonationAsync(Guid donorId, CompleteDonationDto dto);
    Task<IEnumerable<DonationHistoryDto>> GetDonationHistoryAsync(Guid donorId);
    Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(string? status);
    Task<BloodRequestResponseDto> CreateRequestAsync(Guid userId, CreateBloodRequestDto dto);
    Task<(BloodRequestResponseDto? Result, string? Error)> ProcessRequestAsync(Guid id, ProcessRequestDto dto);
    Task<IEnumerable<ComponentCollectionDto>> GetComponentCollectionsAsync();
    Task<(ComponentCollectionDto? Result, string? Error)> CreateComponentCollectionAsync(CreateComponentCollectionDto dto);
    Task<(ComponentCollectionDto? Result, string? Error)> UpdateComponentCollectionAsync(Guid id, CreateComponentCollectionDto dto);
    Task<bool> DeleteComponentCollectionAsync(Guid id);
    Task<IEnumerable<ComponentDiscardDto>> GetComponentDiscardsAsync();
    Task<(ComponentDiscardDto? Result, string? Error)> CreateComponentDiscardAsync(CreateComponentDiscardDto dto);
    Task<(ComponentDiscardDto? Result, string? Error)> ReviewComponentDiscardAsync(Guid id, bool approve, string approvedBy);
    Task<bool> DeleteComponentDiscardAsync(Guid id);
    Task<IEnumerable<ComponentReturnDto>> GetComponentReturnsAsync();
    Task<(ComponentReturnDto? Result, string? Error)> CreateComponentReturnAsync(CreateComponentReturnDto dto);
    Task<bool> DeleteComponentReturnAsync(Guid id);
}
