using BloodBank.API.DTOs;
namespace BloodBank.API.Services;
public interface IBloodInventoryService { Task<IEnumerable<BloodInventoryResponseDto>> GetAllAsync(string? group); Task<BloodInventoryResponseDto> AddOrUpdateAsync(BloodInventoryCreateDto dto); }
