using BloodBank.API.DTOs;
using BloodBank.API.Models;
using BloodBank.API.Repositories;

namespace BloodBank.API.Services;

public sealed class HospitalService : IHospitalService
{
    private readonly IBloodBankRepository _r;
    public HospitalService(IBloodBankRepository repository)
    {
        _r = repository;
    }

    public async Task<BloodRequestResponseDto> CreateRequestAsync(Guid id, CreateBloodRequestDto dto)
    {
        var user = await _r.FindUserAsync(id);
        var request = new BloodRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = id,
            RequesterType = "Hospital",
            RequesterName = user?.HospitalName ?? user?.FullName ?? "Partner Hospital",
            RequesterContact = user?.Phone ?? user?.Email,
            HospitalName = dto.HospitalName?.Trim() ?? user?.HospitalName ?? user?.FullName,
            PatientName = dto.PatientName.Trim(),
            BloodGroup = dto.BloodGroup.Trim().ToUpperInvariant(),
            ComponentType = dto.ComponentType.Trim(),
            QuantityUnits = dto.QuantityUnits,
            Urgency = dto.Urgency,
            Status = "Pending",
            HospitalDepartment = dto.HospitalDepartment?.Trim(),
            MedicalReason = dto.MedicalReason?.Trim(),
            Indication = dto.Indication?.Trim(),
            EmergencyUrgency = dto.EmergencyUrgency?.Trim(),
            RefDoctor = dto.RefDoctor?.Trim(),
            IpNumber = dto.IpNumber?.Trim(),
            RoomNumber = dto.RoomNumber?.Trim(),
            PreviousTransfusion = dto.PreviousTransfusion,
            TransfusionReactionHistory = dto.TransfusionReactionHistory,
            HbGdl = dto.HbGdl,
            PcvPercent = dto.PcvPercent,
            PlateletCount = dto.PlateletCount,
            IndicationDiagnosis = dto.IndicationDiagnosis?.Trim(),
            CrossMatchingMethod = dto.CrossMatchingMethod?.Trim(),
            CrossMatchingReport = dto.CrossMatchingReport?.Trim(),
            BillNumber = dto.BillNumber?.Trim(),
            RequiredResponseTime = dto.RequiredResponseTime?.Trim(),
            RequiredDateTime = dto.RequiredDateTime,
            IssueOrReserve = dto.IssueOrReserve?.Trim(),
            ConsentForIssue = dto.ConsentForIssue,
            ConsentTakenBy = dto.ConsentTakenBy?.Trim(),
            ConsentDate = dto.ConsentDate,
            BloodBankUseOnly = dto.BloodBankUseOnly,
            CreatedAt = DateTime.UtcNow
        };
        var existing = await _r.GetRequestsAsync();
        request.YearlySlNo = existing.Count(item => item.CreatedAt.Year == request.CreatedAt.Year) + 1;
        request.MonthlySlNo = existing.Count(item => item.CreatedAt.Year == request.CreatedAt.Year && item.CreatedAt.Month == request.CreatedAt.Month) + 1;

        await _r.AddRequestAsync(request);
        await _r.SaveChangesAsync();
        return Mapping.Request(request);
    }

    public async Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(Guid id)
    {
        var requests = await _r.GetRequestsAsync(id);
        return requests.Select(Mapping.Request);
    }

    public async Task<IEnumerable<BloodStockDto>> GetStockAsync()
    {
        var stocks = await _r.GetStocksAsync(null, null);
        return stocks
            .Where(stock => stock.QuantityUnits > 0)
            .Select(stock => new BloodStockDto
            {
                Id = stock.Id,
                BloodGroup = stock.BloodGroup,
                ComponentType = stock.ComponentType,
                QuantityUnits = stock.QuantityUnits,
                BloodBankName = stock.BloodBankName,
                LastUpdated = stock.LastUpdated
            });
    }
}

public sealed class DonorService : IDonorService
{
    private readonly IBloodBankRepository _r;
    public DonorService(IBloodBankRepository repository)
    {
        _r = repository;
    }

    public Task<IEnumerable<DonationCamp>> GetCampsAsync()
    {
        return _r.GetCampsAsync(true)
            .ContinueWith(task => (IEnumerable<DonationCamp>)task.Result);
    }

    public async Task<BloodRequestResponseDto> CreateRequestAsync(Guid id, CreateBloodRequestDto dto)
    {
        var user = await _r.FindUserAsync(id);
        var request = new BloodRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = id,
            RequesterType = "Donor",
            RequesterName = user?.FullName ?? "Registered Donor",
            RequesterContact = user?.Phone ?? user?.Email,
            PatientName = dto.PatientName.Trim(),
            BloodGroup = dto.BloodGroup.Trim().ToUpperInvariant(),
            ComponentType = dto.ComponentType.Trim(),
            QuantityUnits = dto.QuantityUnits,
            Urgency = dto.Urgency,
            Status = "Pending",
            HospitalDepartment = dto.HospitalDepartment,
            MedicalReason = dto.MedicalReason,
            Indication = dto.Indication?.Trim(),
            EmergencyUrgency = dto.EmergencyUrgency?.Trim(),
            RefDoctor = dto.RefDoctor?.Trim(),
            IpNumber = dto.IpNumber?.Trim(),
            RoomNumber = dto.RoomNumber?.Trim(),
            PreviousTransfusion = dto.PreviousTransfusion,
            TransfusionReactionHistory = dto.TransfusionReactionHistory,
            HbGdl = dto.HbGdl,
            PcvPercent = dto.PcvPercent,
            PlateletCount = dto.PlateletCount,
            IndicationDiagnosis = dto.IndicationDiagnosis?.Trim(),
            CrossMatchingMethod = dto.CrossMatchingMethod?.Trim(),
            CrossMatchingReport = dto.CrossMatchingReport?.Trim(),
            BillNumber = dto.BillNumber?.Trim(),
            RequiredResponseTime = dto.RequiredResponseTime?.Trim(),
            RequiredDateTime = dto.RequiredDateTime,
            IssueOrReserve = dto.IssueOrReserve?.Trim(),
            ConsentForIssue = dto.ConsentForIssue,
            ConsentTakenBy = dto.ConsentTakenBy?.Trim(),
            ConsentDate = dto.ConsentDate,
            BloodBankUseOnly = dto.BloodBankUseOnly,
            CreatedAt = DateTime.UtcNow
        };
        var existingDonorRequests = await _r.GetRequestsAsync();
        request.YearlySlNo = existingDonorRequests.Count(item => item.CreatedAt.Year == request.CreatedAt.Year) + 1;
        request.MonthlySlNo = existingDonorRequests.Count(item => item.CreatedAt.Year == request.CreatedAt.Year && item.CreatedAt.Month == request.CreatedAt.Month) + 1;

        await _r.AddRequestAsync(request);
        await _r.SaveChangesAsync();
        return Mapping.Request(request);
    }

    public async Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(Guid id)
    {
        var requests = await _r.GetRequestsAsync(id);
        return requests.Select(Mapping.Request);
    }

    public async Task<DonorDetailsDto?> GetProfileAsync(Guid id)
    {
        var user = await _r.FindUserAsync(id);
        if (user == null)
        {
            return null;
        }

        var donations = await _r.GetDonationsAsync(new[] { id });
        var lastDonation = donations
            .OrderByDescending(donation => donation.DonationDate)
            .FirstOrDefault();

        return new DonorDetailsDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            DateOfBirth = user.DateOfBirth,
            Sex = user.Sex,
            Address = user.Address,
            BloodGroup = user.BloodGroup,
            RegisteredDate = user.CreatedAt,
            TotalDonations = donations.Count,
            LastDonationDate = lastDonation?.DonationDate
        };
    }
}

public sealed class BloodInventoryService : IBloodInventoryService
{
    private readonly IBloodBankRepository _r;
    public BloodInventoryService(IBloodBankRepository repository)
    {
        _r = repository;
    }

    public async Task<IEnumerable<BloodInventoryResponseDto>> GetAllAsync(string? bloodGroup)
    {
        var normalizedBloodGroup = string.IsNullOrWhiteSpace(bloodGroup)
            ? null
            : bloodGroup.Trim().ToUpperInvariant();
        var inventories = await _r.GetInventoryAsync(normalizedBloodGroup);

        return inventories.Select(inventory => new BloodInventoryResponseDto
        {
            Id = inventory.Id,
            BloodGroup = inventory.BloodGroup,
            QuantityUnits = inventory.QuantityUnits,
            BloodBankName = inventory.BloodBankName,
            LastUpdated = inventory.LastUpdated
        });
    }

    public async Task<BloodInventoryResponseDto> AddOrUpdateAsync(BloodInventoryCreateDto dto)
    {
        var bloodGroup = dto.BloodGroup.Trim().ToUpperInvariant();
        var bankName = dto.BloodBankName.Trim();
        var inventory = await _r.FindInventoryAsync(bloodGroup, bankName);

        if (inventory == null)
        {
            inventory = new BloodInventory
            {
                Id = Guid.NewGuid(),
                BloodGroup = bloodGroup,
                QuantityUnits = dto.QuantityUnits,
                BloodBankName = bankName,
                LastUpdated = DateTime.UtcNow
            };
            await _r.AddInventoryAsync(inventory);
        }
        else
        {
            inventory.QuantityUnits += dto.QuantityUnits;
            inventory.LastUpdated = DateTime.UtcNow;
        }

        await _r.SaveChangesAsync();
        return new BloodInventoryResponseDto
        {
            Id = inventory.Id,
            BloodGroup = inventory.BloodGroup,
            QuantityUnits = inventory.QuantityUnits,
            BloodBankName = inventory.BloodBankName,
            LastUpdated = inventory.LastUpdated
        };
    }
}
