using BloodBank.API.DTOs;
using BloodBank.API.Models;
namespace BloodBank.API.Services;
public interface IDonorService { Task<IEnumerable<DonationCamp>> GetCampsAsync(); Task<BloodRequestResponseDto> CreateRequestAsync(Guid userId, CreateBloodRequestDto dto); Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(Guid userId); Task<DonorDetailsDto?> GetProfileAsync(Guid userId); }
