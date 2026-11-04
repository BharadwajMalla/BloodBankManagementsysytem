using BloodBank.API.DTOs;
using BloodBank.API.Models;

namespace BloodBank.API.Services;
public interface IBloodBankService
{
    Task<IEnumerable<BloodStockDto>> GetStockAsync(string? group, string? component);
    Task<BloodStockDto?> AddStockAsync(AddStockDto dto);
    Task<bool> BagExistsAsync(string number);
    Task<IEnumerable<BloodBagDto>> GetBagsAsync(string? component);
    Task<IEnumerable<DonationCamp>> GetCampsAsync();
    Task<DonationCamp?> GetCampAsync(Guid id);
    Task<DonationCamp> CreateCampAsync(CreateCampDto dto);
    Task<DonationCamp?> UpdateCampAsync(Guid id, CreateCampDto dto);
    Task<bool> DeleteCampAsync(Guid id);
    Task<IEnumerable<DonorDetailsDto>> GetDonorsAsync();
    Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(string? status);
    Task<BloodRequestResponseDto> CreateRequestAsync(Guid userId, CreateBloodRequestDto dto);
    Task<(BloodRequestResponseDto? Result, string? Error)> ProcessRequestAsync(Guid id, ProcessRequestDto dto);
}
