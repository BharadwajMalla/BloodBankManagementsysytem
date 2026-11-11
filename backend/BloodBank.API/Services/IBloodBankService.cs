using BloodBank.API.DTOs;
using BloodBank.API.Models;

namespace BloodBank.API.Services;
public interface IBloodBankService
{
    Task<IEnumerable<BloodStockDto>> GetStockAsync(string? group, string? component);
    Task<BloodStockDto?> AddStockAsync(AddStockDto dto);
    Task<BloodStockDto> ReplenishStockAsync(ReplenishStockDto dto);
    Task<BloodStockDto?> UpdateStockAsync(Guid id, UpdateStockDto dto);
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
    Task<DonorDetailsDto?> UpdateDonorAsync(Guid id, UpdateDonorDto dto);
    Task<DonationCompletionResultDto> CompleteDonationAsync(Guid donorId, CompleteDonationDto dto);
    Task<IEnumerable<DonationHistoryDto>> GetDonationHistoryAsync(Guid donorId);
    Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(string? status);
    Task<BloodRequestResponseDto> CreateRequestAsync(Guid userId, CreateBloodRequestDto dto);
    Task<BloodRequestResponseDto?> UpdateRequestAsync(Guid id, UpdateBloodRequestDto dto);
    Task<(BloodRequestResponseDto? Result, string? Error)> ProcessRequestAsync(Guid id, ProcessRequestDto dto);
    Task<IEnumerable<ComponentCollectionDto>> GetComponentCollectionsAsync();
    Task<(ComponentCollectionDto? Result, string? Error)> CreateComponentCollectionAsync(CreateComponentCollectionDto dto);
    Task<(ComponentCollectionDto? Result, string? Error)> UpdateComponentCollectionAsync(Guid id, CreateComponentCollectionDto dto);
    Task<bool> DeleteComponentCollectionAsync(Guid id);
    Task<IEnumerable<ComponentDiscardDto>> GetComponentDiscardsAsync();
    Task<(ComponentDiscardDto? Result, string? Error)> CreateComponentDiscardAsync(CreateComponentDiscardDto dto);
    Task<(ComponentDiscardDto? Result, string? Error)> UpdateComponentDiscardAsync(Guid id, UpdateComponentDiscardDto dto);
    Task<(ComponentDiscardDto? Result, string? Error)> ReviewComponentDiscardAsync(Guid id, bool approve, string approvedBy);
    Task<bool> DeleteComponentDiscardAsync(Guid id);
    Task<IEnumerable<ComponentReturnDto>> GetComponentReturnsAsync();
    Task<(ComponentReturnDto? Result, string? Error)> CreateComponentReturnAsync(CreateComponentReturnDto dto);
    Task<(ComponentReturnDto? Result, string? Error)> UpdateComponentReturnAsync(Guid id, UpdateComponentReturnDto dto);
    Task<bool> DeleteComponentReturnAsync(Guid id);

    // Uddanam Blood Centre Register Services
    Task<IEnumerable<TtiScreeningDto>> GetTtiScreeningsAsync(string? status = null);
    Task<TtiScreeningDto> CreateTtiScreeningAsync(CreateTtiScreeningDto dto);
    Task<TtiScreeningDto?> UpdateTtiScreeningAsync(Guid id, UpdateTtiScreeningDto dto);
    Task<bool> DeleteTtiScreeningAsync(Guid id);

    Task<IEnumerable<PatientGroupingDto>> GetPatientGroupingsAsync();
    Task<PatientGroupingDto> CreatePatientGroupingAsync(CreatePatientGroupingDto dto);
    Task<PatientGroupingDto?> UpdatePatientGroupingAsync(Guid id, UpdatePatientGroupingDto dto);
    Task<bool> DeletePatientGroupingAsync(Guid id);

    Task<IEnumerable<BloodBagStockRegisterDto>> GetBloodBagStockRegistersAsync();
    Task<BloodBagStockRegisterDto> CreateBloodBagStockRegisterAsync(CreateBloodBagStockRegisterDto dto);
    Task<BloodBagStockRegisterDto?> UpdateBloodBagStockRegisterAsync(Guid id, UpdateBloodBagStockRegisterDto dto);
    Task<bool> DeleteBloodBagStockRegisterAsync(Guid id);

    Task<IEnumerable<DiagnosticKitRegisterDto>> GetDiagnosticKitRegistersAsync();
    Task<DiagnosticKitRegisterDto> CreateDiagnosticKitRegisterAsync(CreateDiagnosticKitRegisterDto dto);
    Task<DiagnosticKitRegisterDto?> UpdateDiagnosticKitRegisterAsync(Guid id, UpdateDiagnosticKitRegisterDto dto);
    Task<bool> DeleteDiagnosticKitRegisterAsync(Guid id);

    Task<IEnumerable<ApheresisRecordDto>> GetApheresisRecordsAsync();
    Task<ApheresisRecordDto> CreateApheresisRecordAsync(CreateApheresisRecordDto dto);
    Task<ApheresisRecordDto?> UpdateApheresisRecordAsync(Guid id, UpdateApheresisRecordDto dto);
    Task<bool> DeleteApheresisRecordAsync(Guid id);

    Task<IEnumerable<MasterRecordItemDto>> GetMasterRecordsAsync();
}
