using BloodBank.API.DTOs;
namespace BloodBank.API.Services;
public interface IHospitalService { Task<BloodRequestResponseDto> CreateRequestAsync(Guid userId, CreateBloodRequestDto dto); Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(Guid userId); Task<IEnumerable<BloodStockDto>> GetStockAsync(); }
