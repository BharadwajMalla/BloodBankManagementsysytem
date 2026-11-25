using BloodBank.API.DTOs;
using BloodBank.API.Mapping;
using BloodBank.API.Models;
using BloodBank.API.Repositories;
using BloodBank.Application.Contracts;
using System.Text.Json;

namespace BloodBank.API.Services;
internal static class Mapping
{
    public static BloodRequestResponseDto Request(BloodRequest request)
    {
        return new BloodRequestResponseDto
        {
            Id = request.Id,
            RequesterId = request.RequesterId,
            RequesterType = request.RequesterType,
            RequesterName = request.RequesterName,
            RequesterContact = request.RequesterContact,
            HospitalName = request.HospitalName,
            PatientName = request.PatientName,
            PatientAge = request.PatientAge,
            PatientAgeUnit = request.PatientAgeUnit,
            PatientSex = request.PatientSex,
            IsThalassemiaPatient = request.IsThalassemiaPatient,
            BloodGroup = request.BloodGroup,
            ComponentType = request.ComponentType,
            QuantityUnits = request.QuantityUnits,
            Urgency = request.Urgency,
            Status = request.Status,
            HospitalDepartment = request.HospitalDepartment,
            MedicalReason = request.MedicalReason,
            AdminComments = request.AdminComments,
            CreatedAt = request.CreatedAt,
            ProcessedAt = request.ProcessedAt,
            IssuedBagNumbers = request.IssuedBagNumbers,
            IssuedBags = string.IsNullOrWhiteSpace(request.IssuedBagDetailsJson)
                ? new List<IssuedBloodBagDto>()
                : JsonSerializer.Deserialize<List<IssuedBloodBagDto>>(request.IssuedBagDetailsJson) ?? new List<IssuedBloodBagDto>(),
            PaymentMode = request.PaymentMode,
            PaymentStatus = request.PaymentStatus,
            TransactionId = request.TransactionId,
            AmountPaid = request.AmountPaid,
            DiscountAmount = request.DiscountAmount,
            DiscountDonorId = request.DiscountDonorId,
            Indication = request.Indication,
            EmergencyUrgency = request.EmergencyUrgency,
            RefDoctor = request.RefDoctor,
            IpNumber = request.IpNumber,
            HbGdl = request.HbGdl,
            PcvPercent = request.PcvPercent,
            PlateletCount = request.PlateletCount,
            IndicationDiagnosis = request.IndicationDiagnosis,
            RequiredResponseTime = request.RequiredResponseTime,
            RequiredDateTime = request.RequiredDateTime,
            IssueOrReserve = request.IssueOrReserve,
            CrossMatchingMethod = request.CrossMatchingMethod,
            CrossMatchingReport = request.CrossMatchingReport,
            BillNumber = request.BillNumber,
            ConsentForIssue = request.ConsentForIssue,
            ConsentTakenBy = request.ConsentTakenBy,
            ConsentDate = request.ConsentDate,
            BloodBankUseOnly = request.BloodBankUseOnly,
            IssueSerial = request.IssueSerial,
            ReceiverName = request.ReceiverName,
            IssuedByName = request.IssuedByName,
            BloodBankOfficerSignOff = request.BloodBankOfficerSignOff
        };
    }
}
public sealed class BloodBankService : IBloodBankService
{
    private readonly IBloodBankRepository _repo;
    private readonly IDonorDonationService _donorDonationService;

    public BloodBankService(IBloodBankRepository repo, IDonorDonationService donorDonationService)
    {
        _repo = repo;
        _donorDonationService = donorDonationService;
    }

    private static DateTime GetBagExpiry(string componentType, DateTime collectionDate)
    {
        var days = componentType.Contains("Platelet", StringComparison.OrdinalIgnoreCase) ? 5
            : componentType.Contains("Whole", StringComparison.OrdinalIgnoreCase) ? 35
            : componentType.Contains("Plasma", StringComparison.OrdinalIgnoreCase) ? 365
            : 42;
        return collectionDate.AddDays(days);
    }

    public async Task<IEnumerable<BloodStockDto>> GetStockAsync(string? group, string? component)
    {
        var stocks = await _repo.GetStocksAsync(
            string.IsNullOrWhiteSpace(group) ? null : group.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(component) ? null : component.Trim());

        return stocks.Select(stock => new BloodStockDto
        {
            Id = stock.Id,
            BloodGroup = stock.BloodGroup,
            ComponentType = stock.ComponentType,
            QuantityUnits = stock.QuantityUnits,
            BloodBankName = stock.BloodBankName,
            LastUpdated = stock.LastUpdated
        });
    }
    public async Task<BloodStockDto?> AddStockAsync(AddStockDto dto)
    {
        var bloodGroup = dto.BloodGroup.Trim().ToUpperInvariant();
        var componentType = dto.ComponentType.Trim();
        var bagNumber = dto.BagNumber.Trim();
        if (await _repo.BagNumberExistsAsync(bagNumber))
        {
            return null;
        }

        await _repo.AddBagAsync(new BloodBag
        {
            BagNumber = bagNumber,
            BloodGroup = bloodGroup,
            ComponentType = componentType,
            AddedDate = dto.AddedDate == default ? DateTime.UtcNow : dto.AddedDate,
            ExpiryDate = GetBagExpiry(componentType, dto.AddedDate == default ? DateTime.UtcNow : dto.AddedDate),
            Status = "Available",
            ScreeningStatus = "Unscreened"
        });

        var stock = await _repo.FindStockAsync(bloodGroup, componentType);
        if (stock == null)
        {
            stock = new BloodStock
            {
                Id = Guid.NewGuid(),
                BloodGroup = bloodGroup,
                ComponentType = componentType,
                QuantityUnits = 1,
                BloodBankName = "Blood Bank A",
                LastUpdated = DateTime.UtcNow
            };
            await _repo.AddStockAsync(stock);
        }
        else
        {
            stock.QuantityUnits++;
            stock.LastUpdated = DateTime.UtcNow;
        }

        await _repo.SaveChangesAsync();
        return new BloodStockDto
        {
            Id = stock.Id,
            BloodGroup = stock.BloodGroup,
            ComponentType = stock.ComponentType,
            QuantityUnits = stock.QuantityUnits,
            BloodBankName = stock.BloodBankName,
            LastUpdated = stock.LastUpdated
        };
    }
    public async Task<BloodStockDto> ReplenishStockAsync(ReplenishStockDto d)
    {
        var g = d.BloodGroup.Trim().ToUpperInvariant();
        var c = d.ComponentType.Trim();
        var s = await _repo.FindStockAsync(g, c);
        if (s == null)
        {
            s = new BloodStock { Id = Guid.NewGuid(), BloodGroup = g, ComponentType = c, QuantityUnits = d.QuantityUnits, BloodBankName = "Blood Bank A", LastUpdated = DateTime.UtcNow };
            await _repo.AddStockAsync(s);
        }
        else
        {
            s.QuantityUnits += d.QuantityUnits;
            s.LastUpdated = DateTime.UtcNow;
        }

        // Auto-create matching bag barcodes for the added units
        for (int i = 0; i < d.QuantityUnits; i++)
        {
            var bagNum = $"BAG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            await _repo.AddBagAsync(new BloodBag
            {
                BagNumber = bagNum,
                BloodGroup = g,
                ComponentType = c,
                QuantityUnits = 1,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = GetBagExpiry(c, DateTime.UtcNow),
                Status = "Available",
                ScreeningStatus = "Unscreened"
            });
        }

        await _repo.SaveChangesAsync();
        return new BloodStockDto { Id = s.Id, BloodGroup = s.BloodGroup, ComponentType = s.ComponentType, QuantityUnits = s.QuantityUnits, BloodBankName = s.BloodBankName, LastUpdated = s.LastUpdated };
    }
    public Task ClearAllDataAsync()
    {
        return _repo.ClearOperationalDataAsync();
    }

    public Task<bool> BagExistsAsync(string number)
    {
        return _repo.BagNumberExistsAsync(number);
    }

    public async Task<IEnumerable<BloodBagDto>> GetBagsAsync(string? componentType)
    {
        var bags = await _repo.GetBagsAsync(
            string.IsNullOrWhiteSpace(componentType) ? null : componentType.Trim());

        return bags.Select(bag => new BloodBagDto
        {
            Id = bag.Id,
            BagNumber = bag.BagNumber,
            BloodGroup = bag.BloodGroup,
            ComponentType = bag.ComponentType,
            QuantityUnits = bag.QuantityUnits,
            AddedDate = bag.AddedDate,
            Status = bag.Status,
            ExpiryDate = bag.ExpiryDate,
            ScreeningStatus = bag.ScreeningStatus,
            IssuedAt = bag.IssuedAt,
            IssuedBy = bag.IssuedBy
        });
    }

    public async Task<IEnumerable<DonationCamp>> GetCampsAsync()
    {
        return await _repo.GetCampsAsync();
    }

    public async Task<DonationCamp?> GetCampAsync(Guid id)
    {
        return await _repo.FindCampAsync(id);
    }

    public async Task<DonationCamp> CreateCampAsync(CreateCampDto dto)
    {
        var camp = new DonationCamp
        {
            Id = Guid.NewGuid(),
            CampName = dto.CampName.Trim(),
            Location = dto.Location.Trim(),
            CampDate = dto.CampDate,
            TargetUnits = dto.TargetUnits,
            OrganizedBy = "Blood Bank A",
            ContactPhone = dto.ContactPhone?.Trim() ?? "",
            Notes = dto.Notes,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddCampAsync(camp);
        await _repo.SaveChangesAsync();
        return camp;
    }

    public async Task<DonationCamp?> UpdateCampAsync(Guid id, CreateCampDto dto)
    {
        var camp = await _repo.FindCampAsync(id);
        if (camp == null)
        {
            return null;
        }

        camp.CampName = dto.CampName.Trim();
        camp.Location = dto.Location.Trim();
        camp.CampDate = dto.CampDate;
        camp.TargetUnits = dto.TargetUnits;
        camp.ContactPhone = dto.ContactPhone?.Trim() ?? "";
        camp.Notes = dto.Notes;

        await _repo.SaveChangesAsync();
        return camp;
    }

    public async Task<bool> DeleteCampAsync(Guid id)
    {
        var camp = await _repo.FindCampAsync(id);
        if (camp == null)
        {
            return false;
        }

        _repo.RemoveCamp(camp);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<DonorDetailsDto>> GetDonorsAsync()
    {
        var donors = await _donorDonationService.GetDonorsAsync(CancellationToken.None);
        return donors.Select(donor => DonorDonationApiMapper.ToApi(donor)).ToList();
    }
    public async Task<CreateDonorResponseDto?> CreateDonorAsync(CreateDonorDto d)
    {
        var result = await _donorDonationService.CreateDonorAsync(
            DonorDonationApiMapper.ToCommand(d),
            CancellationToken.None);
        return result is null ? null : DonorDonationApiMapper.ToApi(result);
    }

    public async Task<DonationCompletionResultDto> CompleteDonationAsync(Guid donorId, CompleteDonationDto d)
    {
        var result = await _donorDonationService.CompleteDonationAsync(
            donorId,
            DonorDonationApiMapper.ToCommand(d),
            CancellationToken.None);
        return DonorDonationApiMapper.ToApi(result);
    }
    public async Task<IEnumerable<DonationHistoryDto>> GetDonationHistoryAsync(Guid donorId)
    {
        var donations = await _donorDonationService.GetDonationHistoryAsync(donorId, CancellationToken.None);
        return donations.Select(donation => DonorDonationApiMapper.ToApi(donation)).ToList();
    }
    public async Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(string? status)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        var requests = await _repo.GetRequestsAsync(status: normalizedStatus);
        return requests.Select(Mapping.Request);
    }

    public async Task<BloodRequestResponseDto> CreateRequestAsync(Guid id, CreateBloodRequestDto dto)
    {
        var user = await _repo.FindUserAsync(id);
        var request = CreateRequest(
            id,
            "BloodBank",
            user?.FullName ?? "Blood Bank A",
            user?.Phone ?? user?.Email,
            dto);
        var existing = await _repo.GetRequestsAsync();
        var now = DateTime.UtcNow;
        request.YearlySlNo = existing.Count(item => item.CreatedAt.Year == now.Year) + 1;
        request.MonthlySlNo = existing.Count(item => item.CreatedAt.Year == now.Year && item.CreatedAt.Month == now.Month) + 1;

        await _repo.AddRequestAsync(request);
        await _repo.SaveChangesAsync();
        return Mapping.Request(request);
    }

    private static BloodRequest CreateRequest(
        Guid id,
        string type,
        string name,
        string? contact,
        CreateBloodRequestDto dto)
    {
        return new BloodRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = id,
            RequesterType = type,
            RequesterName = name,
            RequesterContact = contact,
            HospitalName = dto.HospitalName?.Trim(),
            PatientName = dto.PatientName.Trim(),
            PatientAge = dto.PatientAge,
            PatientAgeUnit = dto.PatientAgeUnit.Trim(),
            PatientSex = dto.PatientSex.Trim(),
            IsThalassemiaPatient = dto.IsThalassemiaPatient ?? false,
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
    }

    private static string? FormatPatientAgeSex(BloodRequest request)
    {
        if (!request.PatientAge.HasValue || string.IsNullOrWhiteSpace(request.PatientAgeUnit))
            return request.PatientSex;

        var unit = request.PatientAgeUnit == "Months"
            ? request.PatientAge.Value == 1 ? "month" : "months"
            : request.PatientAge.Value == 1 ? "year" : "years";
        var age = $"{request.PatientAge.Value} {unit}";
        return string.IsNullOrWhiteSpace(request.PatientSex)
            ? age
            : $"{age} / {request.PatientSex}";
    }

    public async Task<(BloodRequestResponseDto?, string?)> ProcessRequestAsync(Guid id, ProcessRequestDto d, string? actor = null)
    {
        var r = await _repo.FindRequestAsync(id);
        if (r == null) return (null, "Request not found.");

        if (d.Status == "Approved")
        {
            if (r.Status != "Pending")
                return (null, "Only pending requests can be approved for patient grouping.");

            if (await _repo.FindPatientGroupingByRequestIdAsync(r.Id) != null)
                return (null, "A patient grouping record already exists for this request.");

            var groupingRecords = await _repo.GetPatientGroupingsAsync();
            var rhType = r.BloodGroup.EndsWith("-", StringComparison.Ordinal) ? "Negative" : "Positive";
            var grouping = new PatientGroupingRecord
            {
                Id = Guid.NewGuid(),
                BloodRequestId = r.Id,
                SlNo = groupingRecords.Count + 1,
                Date = DateTime.UtcNow,
                PatientName = r.PatientName,
                AgeSex = FormatPatientAgeSex(r),
                HospitalName = r.HospitalName,
                IpNumber = r.IpNumber,
                BloodGroup = r.BloodGroup,
                RequestedComponentType = r.ComponentType,
                RequestedQuantityUnits = r.QuantityUnits,
                RhType = rhType,
                AntiA = "Not tested",
                AntiB = "Not tested",
                AntiAb = "Not tested",
                AntiD = "Not tested",
                LectinA1 = "Not tested",
                LectinH = "Not tested",
                ACells = "Not tested",
                BCells = "Not tested",
                OCells = "Not tested",
                DuTest = "Not tested",
                IrregularAntibodies = "Not tested",
                AutoAntibodies = "Not tested",
                CrossmatchSaline = "Not tested",
                CrossmatchAlbumin = "Not tested",
                CrossmatchAhg = "Not tested",
                ReviewStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            r.Status = "Processing";
            if (d.AdminComments != null) r.AdminComments = d.AdminComments.Trim();
            r.ProcessedAt = null;
            await _repo.AddPatientGroupingAsync(grouping);
            await _repo.UpdateRequestAsync(r);
            await _repo.SaveChangesAsync();
            return (Mapping.Request(r), null);
        }

        if (d.Status == "Rejected")
        {
            if (r.Status != "Pending")
                return (null, "Only pending requests can be rejected at the initial review stage.");
            if (string.IsNullOrWhiteSpace(d.AdminComments))
                return (null, "A rejection reason is required.");

            r.Status = "Rejected";
            r.AdminComments = d.AdminComments.Trim();
            r.ProcessedAt = DateTime.UtcNow;
            await _repo.UpdateRequestAsync(r);
            await _repo.SaveChangesAsync();
            return (Mapping.Request(r), null);
        }

        if (d.Status != "Fulfilled")
            return (null, "Unsupported request status transition.");

        if (r.Status != "Approved")
            return (null, "The request must pass patient grouping and crossmatch approval before components can be issued.");

        var approvedGrouping = await _repo.FindPatientGroupingByRequestIdAsync(r.Id);
        if (approvedGrouping == null || approvedGrouping.ReviewStatus != "Approved")
            return (null, "Patient grouping and crossmatch approval is required before issuing components.");

        var baseAmount = GetDefaultComponentPrice(r.ComponentType) * r.QuantityUnits;
        var isThalassemiaPatient = r.IsThalassemiaPatient;
        var donorId = string.IsNullOrWhiteSpace(d.DiscountDonorId)
            ? null
            : d.DiscountDonorId.Trim();
        if (!isThalassemiaPatient)
        {
            if (d.PaymentMode is not ("Cash" or "UPI"))
                return (null, "Select Cash or UPI payment before fulfilling the request.");
            if (!string.IsNullOrWhiteSpace(donorId) && !await _repo.DonorIdExistsAsync(donorId))
                return (null, "The donor ID for the discount was not found or is inactive.");
            if (d.PaymentMode == "UPI" && string.IsNullOrWhiteSpace(d.TransactionId))
                return (null, "UPI transaction ID is required before completing payment.");
            if (d.PaymentMode == "UPI"
                && (d.TransactionId!.Trim().Length != 12 || d.TransactionId.Trim().Any(character => !char.IsAsciiDigit(character))))
                return (null, "UPI transaction ID must contain exactly 12 digits.");
        }

        var stock = await _repo.FindStockAsync(r.BloodGroup, r.ComponentType);
        if (stock == null || stock.QuantityUnits < r.QuantityUnits)
            return (null, $"Insufficient stock of {r.BloodGroup} ({r.ComponentType}). Available: {stock?.QuantityUnits ?? 0} units, Requested: {r.QuantityUnits} units.");

        r.DiscountDonorId = isThalassemiaPatient ? null : donorId;
        r.DiscountAmount = isThalassemiaPatient
            ? baseAmount
            : donorId is null ? 0m : Math.Min(baseAmount, DonorProvidedDiscountAmount);
        r.PaymentMode = isThalassemiaPatient ? null : d.PaymentMode;
        r.PaymentStatus = isThalassemiaPatient ? "Waived" : "Completed";
        r.TransactionId = isThalassemiaPatient || d.PaymentMode != "UPI"
            ? null
            : d.TransactionId!.Trim();
        r.AmountPaid = isThalassemiaPatient
            ? 0m
            : !string.IsNullOrWhiteSpace(donorId)
                ? Math.Max(0m, baseAmount - r.DiscountAmount)
                : d.AmountPaid ?? baseAmount;
        if (d.AdminComments != null) r.AdminComments = d.AdminComments.Trim();
        r.ProcessedAt = DateTime.UtcNow;
        if (d.IssueSerial != null) r.IssueSerial = d.IssueSerial.Trim();
        if (d.ReceiverName != null) r.ReceiverName = d.ReceiverName.Trim();
        if (d.IssuedByName != null) r.IssuedByName = d.IssuedByName.Trim();
        if (d.BloodBankOfficerSignOff != null) r.BloodBankOfficerSignOff = d.BloodBankOfficerSignOff.Trim();
        if (d.CrossMatchingMethod != null) r.CrossMatchingMethod = d.CrossMatchingMethod.Trim();
        if (d.CrossMatchingReport != null) r.CrossMatchingReport = d.CrossMatchingReport.Trim();
        if (d.BillNumber != null) r.BillNumber = d.BillNumber.Trim();

        var (_, issueError) = await _repo.TryIssueBagsAtomicallyAsync(r, approvedGrouping, actor);
        if (issueError != null)
            return (null, issueError);

        return (Mapping.Request(r), null);
    }

    private const decimal DonorProvidedDiscountAmount = 450m;

    private static decimal GetDefaultComponentPrice(string componentType)
    {
        return componentType switch
        {
            "Whole Blood" => 1500m,
            "Red Blood Cells (PRBC)" => 1450m,
            "Platelets" => 850m,
            "Fresh Frozen Plasma (FFP)" => 600m,
            "Cryoprecipitate" => 500m,
            _ => 1000m
        };
    }

    private static ComponentCollectionDto MapCollection(ComponentCollection item)
    {
        return new ComponentCollectionDto
        {
            Id = item.Id,
            ComponentType = item.ComponentType,
            BloodGroup = item.BloodGroup,
            BagType = item.BagType,
            VolumeMl = item.VolumeMl,
            BagBarcode = item.BagBarcode,
            CollectionDate = item.CollectionDate,
            ExpiryDate = item.ExpiryDate,
            Status = item.Status
        };
    }

    private static ComponentDiscardDto MapDiscard(ComponentDiscard item)
    {
        return new ComponentDiscardDto
        {
            Id = item.Id,
            BagBarcode = item.BagBarcode,
            Reason = item.Reason,
            DiscardedBy = item.DiscardedBy,
            ApprovalStatus = item.ApprovalStatus,
            ApprovedBy = item.ApprovedBy,
            CreatedAt = item.CreatedAt,
            ApprovedAt = item.ApprovedAt
        };
    }

    private static ComponentReturnDto MapReturn(ComponentReturn item)
    {
        return new ComponentReturnDto
        {
            Id = item.Id,
            BagBarcode = item.BagBarcode,
            Reason = item.Reason,
            ReturnedBy = item.ReturnedBy,
            ReturnDate = item.ReturnDate
        };
    }

    public async Task<IEnumerable<ComponentCollectionDto>> GetComponentCollectionsAsync()
    {
        var collections = await _repo.GetComponentCollectionsAsync();
        return collections.Select(MapCollection);
    }
    public async Task<(ComponentCollectionDto? Result, string? Error)> CreateComponentCollectionAsync(CreateComponentCollectionDto d)
    {
        var barcode = d.BagBarcode.Trim();
        if (await _repo.FindComponentCollectionByBarcodeAsync(barcode) != null || await _repo.BagNumberExistsAsync(barcode))
            return (null, "A component with this bag barcode already exists.");
        if (d.ExpiryDate.Date <= d.CollectionDate.Date)
        {
            return (null, "Expiry date must be after the collection date.");
        }

        var item = new ComponentCollection
        {
            ComponentType = d.ComponentType.Trim(),
            BloodGroup = d.BloodGroup.Trim().ToUpperInvariant(),
            BagType = d.BagType.Trim(),
            VolumeMl = d.VolumeMl,
            BagBarcode = barcode,
            CollectionDate = d.CollectionDate,
            ExpiryDate = d.ExpiryDate,
            Status = "Available"
        };
        await _repo.AddComponentCollectionAsync(item);
        await _repo.AddBagAsync(new BloodBag
        {
            BagNumber = barcode,
            BloodGroup = item.BloodGroup,
            ComponentType = item.ComponentType,
            QuantityUnits = 1,
            AddedDate = item.CollectionDate
        });
        var stock = await _repo.FindStockAsync(item.BloodGroup, item.ComponentType);
        if (stock == null)
        {
            stock = new BloodStock
            {
                Id = Guid.NewGuid(),
                BloodGroup = item.BloodGroup,
                ComponentType = item.ComponentType,
                QuantityUnits = 1,
                BloodBankName = "Blood Bank A",
                LastUpdated = DateTime.UtcNow
            };
            await _repo.AddStockAsync(stock);
        }
        else
        {
            stock.QuantityUnits++;
            stock.LastUpdated = DateTime.UtcNow;
        }
        await _repo.SaveChangesAsync();
        return (MapCollection(item), null);
    }
    public async Task<(ComponentCollectionDto? Result, string? Error)> UpdateComponentCollectionAsync(Guid id, CreateComponentCollectionDto d)
    {
        var item = await _repo.FindComponentCollectionAsync(id);
        if (item == null)
        {
            return (null, "Component collection not found.");
        }
        if (item.Status != "Available")
        {
            return (null, "Only available components can be edited.");
        }
        if (d.ExpiryDate.Date <= d.CollectionDate.Date)
        {
            return (null, "Expiry date must be after the collection date.");
        }
        var bag = await _repo.FindBagByNumberAsync(item.BagBarcode);
        item.ComponentType = d.ComponentType.Trim();
        item.BloodGroup = d.BloodGroup.Trim().ToUpperInvariant();
        item.BagType = d.BagType.Trim();
        item.VolumeMl = d.VolumeMl;
        item.CollectionDate = d.CollectionDate;
        item.ExpiryDate = d.ExpiryDate;
        if (bag != null)
        {
            bag.ComponentType = item.ComponentType;
            bag.BloodGroup = item.BloodGroup;
            bag.AddedDate = item.CollectionDate;
        }
        await _repo.SaveChangesAsync();
        return (MapCollection(item), null);
    }
    public async Task<bool> DeleteComponentCollectionAsync(Guid id)
    {
        var item = await _repo.FindComponentCollectionAsync(id);
        if (item == null || item.Status != "Available")
        {
            return false;
        }

        var bag = await _repo.FindBagByNumberAsync(item.BagBarcode);
        if (bag?.IsIssued == true)
        {
            return false;
        }

        var stock = await _repo.FindStockAsync(item.BloodGroup, item.ComponentType);
        if (stock != null)
        {
            stock.QuantityUnits = Math.Max(0, stock.QuantityUnits - 1);
            stock.LastUpdated = DateTime.UtcNow;
        }

        if (bag != null)
        {
            _repo.RemoveBag(bag);
        }
        _repo.RemoveComponentCollection(item);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ComponentDiscardDto>> GetComponentDiscardsAsync()
    {
        var discards = await _repo.GetComponentDiscardsAsync();
        return discards.Select(MapDiscard);
    }
    public async Task<(ComponentDiscardDto? Result, string? Error)> CreateComponentDiscardAsync(CreateComponentDiscardDto d)
    {
        var item = await _repo.FindComponentCollectionByBarcodeAsync(d.BagBarcode.Trim());
        if (item == null)
        {
            return (null, "Component barcode was not found.");
        }
        if (item.Status != "Available")
        {
            return (null, "Only available components can be submitted for discard.");
        }

        var discard = new ComponentDiscard
        {
            BagBarcode = item.BagBarcode,
            Reason = d.Reason.Trim(),
            DiscardedBy = d.DiscardedBy.Trim()
        };

        await _repo.AddComponentDiscardAsync(discard);
        await _repo.SaveChangesAsync();
        return (MapDiscard(discard), null);
    }
    public async Task<(ComponentDiscardDto? Result, string? Error)> ReviewComponentDiscardAsync(Guid id, bool approve, string approvedBy)
    {
        var discard = await _repo.FindComponentDiscardAsync(id);
        if (discard == null)
        {
            return (null, "Discard request not found.");
        }
        if (discard.ApprovalStatus != "Pending")
        {
            return (null, "This discard request has already been reviewed.");
        }

        discard.ApprovalStatus = approve ? "Approved" : "Rejected";
        discard.ApprovedBy = approvedBy;
        discard.ApprovedAt = DateTime.UtcNow;
        if (approve)
        {
            var item = await _repo.FindComponentCollectionByBarcodeAsync(discard.BagBarcode);
            if (item != null)
            {
                item.Status = "Discarded";
                var stock = await _repo.FindStockAsync(item.BloodGroup, item.ComponentType);
                if (stock != null)
                {
                    stock.QuantityUnits = Math.Max(0, stock.QuantityUnits - 1);
                    stock.LastUpdated = DateTime.UtcNow;
                }
            }
        }

        await _repo.SaveChangesAsync();
        return (MapDiscard(discard), null);
    }

    public async Task<bool> DeleteComponentDiscardAsync(Guid id)
    {
        var item = await _repo.FindComponentDiscardAsync(id);
        if (item == null)
        {
            return false;
        }

        _repo.RemoveComponentDiscard(item);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ComponentReturnDto>> GetComponentReturnsAsync()
    {
        var returns = await _repo.GetComponentReturnsAsync();
        return returns.Select(MapReturn);
    }
    public async Task<(ComponentReturnDto? Result, string? Error)> CreateComponentReturnAsync(CreateComponentReturnDto d)
    {
        var barcode = d.BagBarcode.Trim();
        var bag = await _repo.FindBagByNumberAsync(barcode);
        if (bag == null)
        {
            return (null, "Issued component barcode was not found.");
        }
        if (!bag.IsIssued)
        {
            return (null, "This component is not currently issued to a patient.");
        }

        bag.IsIssued = false;
        bag.IssuedDate = null;
        var item = await _repo.FindComponentCollectionByBarcodeAsync(barcode);
        if (item != null)
        {
            item.Status = "Available";
        }

        var stock = await _repo.FindStockAsync(bag.BloodGroup, bag.ComponentType);
        if (stock == null)
        {
            stock = new BloodStock
            {
                Id = Guid.NewGuid(),
                BloodGroup = bag.BloodGroup,
                ComponentType = bag.ComponentType,
                QuantityUnits = 1,
                BloodBankName = "Blood Bank A",
                LastUpdated = DateTime.UtcNow
            };
            await _repo.AddStockAsync(stock);
        }
        else
        {
            stock.QuantityUnits++;
            stock.LastUpdated = DateTime.UtcNow;
        }

        var returned = new ComponentReturn
        {
            BagBarcode = barcode,
            Reason = d.Reason.Trim(),
            ReturnedBy = d.ReturnedBy.Trim()
        };
        await _repo.AddComponentReturnAsync(returned);
        await _repo.SaveChangesAsync();
        return (MapReturn(returned), null);
    }

    public async Task<BloodStockDto?> UpdateStockAsync(Guid id, UpdateStockDto d)
    {
        var s = await _repo.FindStockByIdAsync(id);
        if (s == null) return null;
        s.QuantityUnits = d.QuantityUnits;
        s.LastUpdated = DateTime.UtcNow;
        await _repo.SaveChangesAsync();
        return new BloodStockDto { Id = s.Id, BloodGroup = s.BloodGroup, ComponentType = s.ComponentType, QuantityUnits = s.QuantityUnits, BloodBankName = s.BloodBankName, LastUpdated = s.LastUpdated };
    }

    public async Task<DonorDetailsDto?> UpdateDonorAsync(Guid id, UpdateDonorDto d)
    {
        var donor = await _donorDonationService.UpdateDonorAsync(
            id,
            DonorDonationApiMapper.ToCommand(d),
            CancellationToken.None);
        return donor is null ? null : DonorDonationApiMapper.ToApi(donor);
    }

    public async Task<BloodRequestResponseDto?> UpdateRequestAsync(Guid id, UpdateBloodRequestDto d)
    {
        var req = await _repo.FindRequestAsync(id);
        if (req == null) return null;
        if (req.Status != "Pending") return null;

        if (!string.IsNullOrWhiteSpace(d.HospitalName)) req.HospitalName = d.HospitalName.Trim();
        if (!string.IsNullOrWhiteSpace(d.PatientName)) req.PatientName = d.PatientName.Trim();
        if (d.PatientAge.HasValue) req.PatientAge = d.PatientAge;
        if (!string.IsNullOrWhiteSpace(d.PatientAgeUnit)) req.PatientAgeUnit = d.PatientAgeUnit.Trim();
        if (!string.IsNullOrWhiteSpace(d.PatientSex)) req.PatientSex = d.PatientSex.Trim();
        if (d.IsThalassemiaPatient.HasValue) req.IsThalassemiaPatient = d.IsThalassemiaPatient.Value;
        if (!string.IsNullOrWhiteSpace(d.BloodGroup)) req.BloodGroup = d.BloodGroup.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(d.ComponentType)) req.ComponentType = d.ComponentType.Trim();
        if (d.QuantityUnits > 0) req.QuantityUnits = d.QuantityUnits;
        if (!string.IsNullOrWhiteSpace(d.Urgency)) req.Urgency = d.Urgency.Trim();
        if (d.HospitalDepartment != null) req.HospitalDepartment = d.HospitalDepartment.Trim();
        if (d.MedicalReason != null) req.MedicalReason = d.MedicalReason.Trim();
        if (!string.IsNullOrWhiteSpace(d.AdminComments)) req.AdminComments = d.AdminComments.Trim();
        if (d.Indication != null) req.Indication = d.Indication.Trim();
        if (d.EmergencyUrgency != null) req.EmergencyUrgency = d.EmergencyUrgency.Trim();
        if (d.RefDoctor != null) req.RefDoctor = d.RefDoctor.Trim();
        if (d.IpNumber != null) req.IpNumber = d.IpNumber.Trim();
        if (d.RoomNumber != null) req.RoomNumber = d.RoomNumber.Trim();
        if (d.PreviousTransfusion.HasValue) req.PreviousTransfusion = d.PreviousTransfusion;
        if (d.TransfusionReactionHistory.HasValue) req.TransfusionReactionHistory = d.TransfusionReactionHistory;
        if (d.HbGdl.HasValue) req.HbGdl = d.HbGdl;
        if (d.PcvPercent.HasValue) req.PcvPercent = d.PcvPercent;
        if (d.PlateletCount.HasValue) req.PlateletCount = d.PlateletCount;
        if (d.IndicationDiagnosis != null) req.IndicationDiagnosis = d.IndicationDiagnosis.Trim();
        if (d.CrossMatchingMethod != null) req.CrossMatchingMethod = d.CrossMatchingMethod.Trim();
        if (d.CrossMatchingReport != null) req.CrossMatchingReport = d.CrossMatchingReport.Trim();
        if (d.BillNumber != null) req.BillNumber = d.BillNumber.Trim();
        if (d.RequiredResponseTime != null) req.RequiredResponseTime = d.RequiredResponseTime.Trim();
        if (d.RequiredDateTime.HasValue) req.RequiredDateTime = d.RequiredDateTime;
        if (d.IssueOrReserve != null) req.IssueOrReserve = d.IssueOrReserve.Trim();
        req.ConsentForIssue = d.ConsentForIssue;
        if (d.ConsentTakenBy != null) req.ConsentTakenBy = d.ConsentTakenBy.Trim();
        if (d.ConsentDate.HasValue) req.ConsentDate = d.ConsentDate;
        req.BloodBankUseOnly = d.BloodBankUseOnly;
        if (d.IssueSerial != null) req.IssueSerial = d.IssueSerial.Trim();
        if (d.ReceiverName != null) req.ReceiverName = d.ReceiverName.Trim();
        if (d.IssuedByName != null) req.IssuedByName = d.IssuedByName.Trim();
        if (d.BloodBankOfficerSignOff != null) req.BloodBankOfficerSignOff = d.BloodBankOfficerSignOff.Trim();

        await _repo.SaveChangesAsync();
        return Mapping.Request(req);
    }

    public async Task<bool?> DeletePendingRequestAsync(Guid id)
    {
        var request = await _repo.FindRequestAsync(id);
        if (request is null) return null;
        if (request.Status != "Pending"
            || request.ProcessedAt.HasValue
            || !string.IsNullOrWhiteSpace(request.IssuedBagNumbers)
            || request.PaymentStatus != "Pending")
        {
            return false;
        }

        return await _repo.DeletePendingRequestAsync(id);
    }

    public async Task<(ComponentDiscardDto? Result, string? Error)> UpdateComponentDiscardAsync(Guid id, UpdateComponentDiscardDto d)
    {
        var item = await _repo.FindComponentDiscardAsync(id);
        if (item == null) return (null, "Discard record not found.");
        if (!string.IsNullOrWhiteSpace(d.BagBarcode)) item.BagBarcode = d.BagBarcode.Trim();
        if (!string.IsNullOrWhiteSpace(d.Reason)) item.Reason = d.Reason.Trim();
        if (!string.IsNullOrWhiteSpace(d.DiscardedBy)) item.DiscardedBy = d.DiscardedBy.Trim();
        if (!string.IsNullOrWhiteSpace(d.ApprovalStatus)) item.ApprovalStatus = d.ApprovalStatus.Trim();
        await _repo.SaveChangesAsync();
        return (MapDiscard(item), null);
    }

    public async Task<(ComponentReturnDto? Result, string? Error)> UpdateComponentReturnAsync(Guid id, UpdateComponentReturnDto d)
    {
        var item = await _repo.FindComponentReturnAsync(id);
        if (item == null) return (null, "Return record not found.");
        if (!string.IsNullOrWhiteSpace(d.BagBarcode)) item.BagBarcode = d.BagBarcode.Trim();
        if (!string.IsNullOrWhiteSpace(d.Reason)) item.Reason = d.Reason.Trim();
        if (!string.IsNullOrWhiteSpace(d.ReturnedBy)) item.ReturnedBy = d.ReturnedBy.Trim();
        await _repo.SaveChangesAsync();
        return (MapReturn(item), null);
    }

    // ==========================================
    // Uddanam Register 1: TTI Screening
    // ==========================================
    private static TtiScreeningDto MapTti(TtiScreeningRecord record)
    {
        return new TtiScreeningDto
        {
            Id = record.Id,
            BagBarcode = record.BagBarcode,
            BloodGroup = record.BloodGroup,
            TestingDate = record.TestingDate,
            HivResult = record.HivResult,
            HivIResult = record.HivIResult,
            HivIiResult = record.HivIiResult,
            HbsAgResult = record.HbsAgResult,
            HcvResult = record.HcvResult,
            VdrlResult = record.VdrlResult,
            MalariaResult = record.MalariaResult,
            AtypicalAntibodies = record.AtypicalAntibodies,
            OverallStatus = record.OverallStatus,
            TestedBy = record.TestedBy,
            ApprovedBy = record.ApprovedBy,
            Remarks = record.Remarks,
            ElisaKitBatch = record.ElisaKitBatch,
            ElisaSignOffBy = record.ElisaSignOffBy,
            ElisaSignOffAt = record.ElisaSignOffAt,
            CreatedAt = record.CreatedAt
        };
    }

    public async Task<IEnumerable<TtiScreeningDto>> GetTtiScreeningsAsync(string? status = null)
    {
        var records = await _repo.GetTtiScreeningsAsync(status);
        return records.Select(MapTti);
    }

    public async Task<TtiScreeningDto> CreateTtiScreeningAsync(CreateTtiScreeningDto d)
    {
        bool isReactive = d.HivResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.HivIResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.HivIiResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.HbsAgResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.HcvResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.VdrlResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.MalariaResult.Contains("Positive", StringComparison.OrdinalIgnoreCase)
            || d.AtypicalAntibodies.Contains("Positive", StringComparison.OrdinalIgnoreCase);

        var overallStatus = isReactive ? "Failed - Reactive" : (string.IsNullOrWhiteSpace(d.OverallStatus) ? "Passed" : d.OverallStatus.Trim());

        var record = new TtiScreeningRecord
        {
            Id = Guid.NewGuid(),
            BagBarcode = d.BagBarcode.Trim(),
            BloodGroup = d.BloodGroup.Trim().ToUpperInvariant(),
            TestingDate = d.TestingDate == default ? DateTime.UtcNow : d.TestingDate,
            HivResult = d.HivResult.Trim(),
            HivIResult = d.HivIResult.Trim(),
            HivIiResult = d.HivIiResult.Trim(),
            HbsAgResult = d.HbsAgResult.Trim(),
            HcvResult = d.HcvResult.Trim(),
            VdrlResult = d.VdrlResult.Trim(),
            MalariaResult = d.MalariaResult.Trim(),
            AtypicalAntibodies = d.AtypicalAntibodies.Trim(),
            OverallStatus = overallStatus,
            TestedBy = d.TestedBy.Trim(),
            ApprovedBy = d.ApprovedBy?.Trim(),
            Remarks = d.Remarks?.Trim(),
            ElisaKitBatch = d.ElisaKitBatch?.Trim(),
            ElisaSignOffBy = d.ElisaSignOffBy?.Trim(),
            ElisaSignOffAt = d.ElisaSignOffAt,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddTtiScreeningAsync(record);

        if (isReactive)
        {
            var bag = await _repo.FindBagByNumberAsync(record.BagBarcode);
            if (bag != null)
            {
                bag.ScreeningStatus = "Reactive";
                bag.Status = "Quarantined";
                await _repo.UpdateBagAsync(bag);
            }
            var colItem = await _repo.FindComponentCollectionByBarcodeAsync(record.BagBarcode);
            if (colItem != null)
            {
                colItem.Status = "Discarded";
                await _repo.UpdateComponentCollectionAsync(colItem);
            }

            var discard = new ComponentDiscard
            {
                Id = Guid.NewGuid(),
                BagBarcode = record.BagBarcode,
                Reason = $"TTI Infectious Marker Reactive: HIV={record.HivResult}, HBsAg={record.HbsAgResult}, HCV={record.HcvResult}, VDRL={record.VdrlResult}, MP={record.MalariaResult}",
                DiscardedBy = record.TestedBy,
                ApprovalStatus = "Approved",
                ApprovedBy = record.ApprovedBy ?? "Medical Officer",
                CreatedAt = DateTime.UtcNow,
                ApprovedAt = DateTime.UtcNow
            };
            await _repo.AddComponentDiscardAsync(discard);
        }
        else
        {
            var bag = await _repo.FindBagByNumberAsync(record.BagBarcode);
            if (bag != null)
            {
                bag.ScreeningStatus = "Passed";
                bag.Status = "Available";
                await _repo.UpdateBagAsync(bag);
            }
        }

        await _repo.SaveChangesAsync();
        return MapTti(record);
    }

    public async Task<TtiScreeningDto?> UpdateTtiScreeningAsync(Guid id, UpdateTtiScreeningDto d)
    {
        var record = await _repo.FindTtiScreeningAsync(id);
        if (record == null) return null;

        record.BagBarcode = d.BagBarcode.Trim();
        record.BloodGroup = d.BloodGroup.Trim().ToUpperInvariant();
        record.TestingDate = d.TestingDate;
        record.HivResult = d.HivResult.Trim();
        record.HivIResult = d.HivIResult.Trim();
        record.HivIiResult = d.HivIiResult.Trim();
        record.HbsAgResult = d.HbsAgResult.Trim();
        record.HcvResult = d.HcvResult.Trim();
        record.VdrlResult = d.VdrlResult.Trim();
        record.MalariaResult = d.MalariaResult.Trim();
        record.AtypicalAntibodies = d.AtypicalAntibodies.Trim();
        record.OverallStatus = d.OverallStatus.Trim();
        record.TestedBy = d.TestedBy.Trim();
        record.ApprovedBy = d.ApprovedBy?.Trim();
        record.Remarks = d.Remarks?.Trim();
        record.ElisaKitBatch = d.ElisaKitBatch?.Trim();
        record.ElisaSignOffBy = d.ElisaSignOffBy?.Trim();
        record.ElisaSignOffAt = d.ElisaSignOffAt;

        var isReactive = d.HivResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.HivIResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.HivIiResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.HbsAgResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.HcvResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.VdrlResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
            || d.MalariaResult.Contains("Positive", StringComparison.OrdinalIgnoreCase)
            || d.AtypicalAntibodies.Contains("Positive", StringComparison.OrdinalIgnoreCase);
        record.OverallStatus = isReactive ? "Failed - Reactive" : (string.IsNullOrWhiteSpace(d.OverallStatus) ? "Passed" : d.OverallStatus.Trim());

        await _repo.UpdateTtiScreeningAsync(record);
        var bag = await _repo.FindBagByNumberAsync(record.BagBarcode);
        if (bag != null)
        {
            bag.ScreeningStatus = isReactive ? "Reactive" : "Passed";
            bag.Status = isReactive ? "Quarantined" : "Available";
            await _repo.UpdateBagAsync(bag);
        }
        await _repo.SaveChangesAsync();
        return MapTti(record);
    }

    public async Task<bool> DeleteTtiScreeningAsync(Guid id)
    {
        var record = await _repo.FindTtiScreeningAsync(id);
        if (record == null) return false;
        _repo.RemoveTtiScreening(record);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ==========================================
    // Uddanam Register 2: Patient Grouping & Crossmatch
    // ==========================================
    private static PatientGroupingDto MapGrouping(PatientGroupingRecord record)
    {
        return new PatientGroupingDto
        {
            Id = record.Id,
            SlNo = record.SlNo,
            Date = record.Date,
            BloodRequestId = record.BloodRequestId,
            RequestedComponentType = record.RequestedComponentType,
            RequestedQuantityUnits = record.RequestedQuantityUnits,
            IssuedBags = string.IsNullOrWhiteSpace(record.IssuedBagDetailsJson)
                ? new List<IssuedBloodBagDto>()
                : JsonSerializer.Deserialize<List<IssuedBloodBagDto>>(record.IssuedBagDetailsJson) ?? new List<IssuedBloodBagDto>(),
            ReviewStatus = record.ReviewStatus,
            RejectionReason = record.RejectionReason,
            ReviewedBy = record.ReviewedBy,
            ReviewedAt = record.ReviewedAt,
            PatientName = record.PatientName,
            AgeSex = record.AgeSex,
            HospitalName = record.HospitalName,
            IpNumber = record.IpNumber,
            BagNumber = record.BagNumber,
            BagType = record.BagType,
            BloodGroup = record.BloodGroup,
            RhType = record.RhType,
            AntiA = record.AntiA,
            AntiB = record.AntiB,
            AntiAb = record.AntiAb,
            AntiD = record.AntiD,
            LectinA1 = record.LectinA1,
            LectinH = record.LectinH,
            ACells = record.ACells,
            BCells = record.BCells,
            OCells = record.OCells,
            DuTest = record.DuTest,
            IrregularAntibodies = record.IrregularAntibodies,
            AutoAntibodies = record.AutoAntibodies,
            CrossmatchSaline = record.CrossmatchSaline,
            CrossmatchAlbumin = record.CrossmatchAlbumin,
            CrossmatchAhg = record.CrossmatchAhg,
            TechnicianSignature = record.TechnicianSignature,
            MedicalOfficerSignature = record.MedicalOfficerSignature,
            CreatedAt = record.CreatedAt
        };
    }

    public async Task<IEnumerable<PatientGroupingDto>> GetPatientGroupingsAsync()
    {
        var records = await _repo.GetPatientGroupingsAsync();
        return records.Select(MapGrouping);
    }

    public async Task<PatientGroupingDto> CreatePatientGroupingAsync(CreatePatientGroupingDto d)
    {
        var existing = await _repo.GetPatientGroupingsAsync();
        var nextSl = existing.Count + 1;

        var item = new PatientGroupingRecord
        {
            Id = Guid.NewGuid(),
            SlNo = d.SlNo > 0 ? d.SlNo : nextSl,
            Date = d.Date == default ? DateTime.UtcNow : d.Date,
            PatientName = d.PatientName.Trim(),
            AgeSex = d.AgeSex?.Trim(),
            HospitalName = d.HospitalName?.Trim(),
            IpNumber = d.IpNumber?.Trim(),
            BagNumber = d.BagNumber?.Trim(),
            BagType = d.BagType?.Trim(),
            BloodGroup = d.BloodGroup.Trim().ToUpperInvariant(),
            RhType = d.RhType.Trim(),
            AntiA = d.AntiA?.Trim() ?? "+",
            AntiB = d.AntiB?.Trim() ?? "-",
            AntiAb = d.AntiAb?.Trim() ?? "+",
            AntiD = d.AntiD?.Trim() ?? "+",
            LectinA1 = d.LectinA1?.Trim() ?? "+",
            LectinH = d.LectinH?.Trim() ?? "-",
            ACells = d.ACells?.Trim() ?? "-",
            BCells = d.BCells?.Trim() ?? "+",
            OCells = d.OCells?.Trim() ?? "-",
            DuTest = d.DuTest?.Trim() ?? "Negative",
            IrregularAntibodies = d.IrregularAntibodies?.Trim() ?? "Negative",
            AutoAntibodies = d.AutoAntibodies?.Trim() ?? "Negative",
            CrossmatchSaline = d.CrossmatchSaline?.Trim() ?? "Compatible",
            CrossmatchAlbumin = d.CrossmatchAlbumin?.Trim() ?? "Compatible",
            CrossmatchAhg = d.CrossmatchAhg?.Trim() ?? "Compatible",
            TechnicianSignature = d.TechnicianSignature?.Trim(),
            MedicalOfficerSignature = d.MedicalOfficerSignature?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddPatientGroupingAsync(item);
        await _repo.SaveChangesAsync();
        return MapGrouping(item);
    }

    public async Task<PatientGroupingDto?> UpdatePatientGroupingAsync(Guid id, UpdatePatientGroupingDto d)
    {
        var item = await _repo.FindPatientGroupingAsync(id);
        if (item == null) return null;
        if (item.BloodRequestId.HasValue && item.ReviewStatus != "Pending") return null;

        item.PatientName = d.PatientName.Trim();
        item.AgeSex = d.AgeSex?.Trim();
        item.HospitalName = d.HospitalName?.Trim();
        item.IpNumber = d.IpNumber?.Trim();
        item.BagNumber = d.BagNumber?.Trim();
        item.BagType = d.BagType?.Trim();
        item.BloodGroup = d.BloodGroup.Trim().ToUpperInvariant();
        item.RhType = d.RhType.Trim();
        item.AntiA = d.AntiA?.Trim() ?? item.AntiA;
        item.AntiB = d.AntiB?.Trim() ?? item.AntiB;
        item.AntiAb = d.AntiAb?.Trim() ?? item.AntiAb;
        item.AntiD = d.AntiD?.Trim() ?? item.AntiD;
        item.LectinA1 = d.LectinA1?.Trim() ?? item.LectinA1;
        item.LectinH = d.LectinH?.Trim() ?? item.LectinH;
        item.ACells = d.ACells?.Trim() ?? item.ACells;
        item.BCells = d.BCells?.Trim() ?? item.BCells;
        item.OCells = d.OCells?.Trim() ?? item.OCells;
        item.DuTest = d.DuTest?.Trim() ?? item.DuTest;
        item.IrregularAntibodies = d.IrregularAntibodies?.Trim() ?? item.IrregularAntibodies;
        item.AutoAntibodies = d.AutoAntibodies?.Trim() ?? item.AutoAntibodies;
        item.CrossmatchSaline = d.CrossmatchSaline?.Trim() ?? item.CrossmatchSaline;
        item.CrossmatchAlbumin = d.CrossmatchAlbumin?.Trim() ?? item.CrossmatchAlbumin;
        item.CrossmatchAhg = d.CrossmatchAhg?.Trim() ?? item.CrossmatchAhg;
        item.TechnicianSignature = d.TechnicianSignature?.Trim();
        item.MedicalOfficerSignature = d.MedicalOfficerSignature?.Trim();

        await _repo.UpdatePatientGroupingAsync(item);
        await _repo.SaveChangesAsync();
        return MapGrouping(item);
    }

    public async Task<(PatientGroupingDto? Result, string? Error)> ReviewPatientGroupingAsync(
        Guid id, ReviewPatientGroupingDto d, string? reviewer = null)
    {
        var item = await _repo.FindPatientGroupingAsync(id);
        if (item == null) return (null, "Patient grouping record not found.");
        if (!item.BloodRequestId.HasValue)
            return (null, "Only request-linked patient grouping records can approve or reject a request.");
        if (item.ReviewStatus != "Pending")
            return (null, "This patient grouping record has already been reviewed.");

        var request = await _repo.FindRequestAsync(item.BloodRequestId.Value);
        if (request == null) return (null, "The blood request linked to this grouping record was not found.");
        if (request.Status != "Processing")
            return (null, "The linked request is not awaiting patient grouping review.");

        var reviewedAt = DateTime.UtcNow;
        if (d.Status == "Rejected")
        {
            if (string.IsNullOrWhiteSpace(d.RejectionReason))
                return (null, "A rejection reason is required.");

            item.ReviewStatus = "Rejected";
            item.RejectionReason = d.RejectionReason.Trim();
            request.Status = "Rejected";
            request.AdminComments = item.RejectionReason;
            request.ProcessedAt = reviewedAt;
        }
        else if (d.Status == "Approved")
        {
            if (!PatientGroupingCompatibility.IsCompatible(item, request.BloodGroup))
                return (null, "Complete all grouping tests, ensure saline, albumin, and AHG crossmatches are Compatible, and verify the patient blood group matches the request.");

            item.ReviewStatus = "Approved";
            item.RejectionReason = null;
            request.Status = "Approved";
        }
        else
        {
            return (null, "Unsupported patient grouping review decision.");
        }

        item.ReviewedBy = reviewer;
        item.ReviewedAt = reviewedAt;
        await _repo.UpdatePatientGroupingAsync(item);
        await _repo.UpdateRequestAsync(request);
        await _repo.SaveChangesAsync();
        return (MapGrouping(item), null);
    }

    public async Task<bool> DeletePatientGroupingAsync(Guid id)
    {
        var item = await _repo.FindPatientGroupingAsync(id);
        if (item == null) return false;
        if (item.BloodRequestId.HasValue) return false;
        _repo.RemovePatientGrouping(item);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ==========================================
    // Uddanam Register 3: CPDA Blood Bags Stock Register
    // ==========================================
    private static BloodBagStockRegisterDto MapBagStock(BloodBagStockRegister record)
    {
        return new BloodBagStockRegisterDto
        {
            Id = record.Id,
            SlNo = record.SlNo,
            Date = record.Date,
            SupplierName = record.SupplierName,
            InvoiceNo = record.InvoiceNo,
            InvoiceDate = record.InvoiceDate,
            Manufacturer = record.Manufacturer,
            BatchNo = record.BatchNo,
            DateOfSupply = record.DateOfSupply,
            DateOfManufacture = record.DateOfManufacture,
            DateOfExpiry = record.DateOfExpiry,
            CodeSpecification = record.CodeSpecification,
            ResultCoa = record.ResultCoa,
            OpeningStock = record.OpeningStock,
            QuantityReceived = record.QuantityReceived,
            QuantityUsed = record.QuantityUsed,
            Wastage = record.Wastage,
            Balance = record.Balance,
            TechSign = record.TechSign,
            MedicalOfficerSign = record.MedicalOfficerSign,
            Remarks = record.Remarks,
            CreatedAt = record.CreatedAt
        };
    }

    public async Task<IEnumerable<BloodBagStockRegisterDto>> GetBloodBagStockRegistersAsync()
    {
        var records = await _repo.GetBloodBagStockRegistersAsync();
        return records.Select(MapBagStock);
    }

    public async Task<BloodBagStockRegisterDto> CreateBloodBagStockRegisterAsync(CreateBloodBagStockRegisterDto d)
    {
        var existing = await _repo.GetBloodBagStockRegistersAsync();
        var nextSl = existing.Count + 1;
        var balance = d.OpeningStock + d.QuantityReceived - d.QuantityUsed - d.Wastage;

        var item = new BloodBagStockRegister
        {
            Id = Guid.NewGuid(),
            SlNo = d.SlNo > 0 ? d.SlNo : nextSl,
            Date = d.Date == default ? DateTime.UtcNow : d.Date,
            SupplierName = d.SupplierName.Trim(),
            InvoiceNo = d.InvoiceNo.Trim(),
            InvoiceDate = d.InvoiceDate,
            Manufacturer = d.Manufacturer.Trim(),
            BatchNo = d.BatchNo.Trim(),
            DateOfSupply = d.DateOfSupply,
            DateOfManufacture = d.DateOfManufacture,
            DateOfExpiry = d.DateOfExpiry,
            CodeSpecification = d.CodeSpecification.Trim(),
            ResultCoa = string.IsNullOrWhiteSpace(d.ResultCoa) ? "Satisfactory" : d.ResultCoa.Trim(),
            OpeningStock = d.OpeningStock,
            QuantityReceived = d.QuantityReceived,
            QuantityUsed = d.QuantityUsed,
            Wastage = d.Wastage,
            Balance = balance,
            TechSign = d.TechSign?.Trim(),
            MedicalOfficerSign = d.MedicalOfficerSign?.Trim(),
            Remarks = d.Remarks?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddBloodBagStockRegisterAsync(item);
        await _repo.SaveChangesAsync();
        return MapBagStock(item);
    }

    public async Task<BloodBagStockRegisterDto?> UpdateBloodBagStockRegisterAsync(Guid id, UpdateBloodBagStockRegisterDto d)
    {
        var item = await _repo.FindBloodBagStockRegisterAsync(id);
        if (item == null) return null;

        item.SupplierName = d.SupplierName.Trim();
        item.InvoiceNo = d.InvoiceNo.Trim();
        item.InvoiceDate = d.InvoiceDate;
        item.Manufacturer = d.Manufacturer.Trim();
        item.BatchNo = d.BatchNo.Trim();
        item.DateOfSupply = d.DateOfSupply;
        item.DateOfManufacture = d.DateOfManufacture;
        item.DateOfExpiry = d.DateOfExpiry;
        item.CodeSpecification = d.CodeSpecification.Trim();
        item.ResultCoa = d.ResultCoa.Trim();
        item.OpeningStock = d.OpeningStock;
        item.QuantityReceived = d.QuantityReceived;
        item.QuantityUsed = d.QuantityUsed;
        item.Wastage = d.Wastage;
        item.Balance = d.OpeningStock + d.QuantityReceived - d.QuantityUsed - d.Wastage;
        item.TechSign = d.TechSign?.Trim();
        item.MedicalOfficerSign = d.MedicalOfficerSign?.Trim();
        item.Remarks = d.Remarks?.Trim();

        await _repo.UpdateBloodBagStockRegisterAsync(item);
        await _repo.SaveChangesAsync();
        return MapBagStock(item);
    }

    public async Task<bool> DeleteBloodBagStockRegisterAsync(Guid id)
    {
        var item = await _repo.FindBloodBagStockRegisterAsync(id);
        if (item == null) return false;
        _repo.RemoveBloodBagStockRegister(item);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ==========================================
    // Uddanam Register 4: Diagnostic Kits & Reagents
    // ==========================================
    private static DiagnosticKitRegisterDto MapKit(DiagnosticKitRegister record)
    {
        return new DiagnosticKitRegisterDto
        {
            Id = record.Id,
            SlNo = record.SlNo,
            Date = record.Date,
            SupplierName = record.SupplierName,
            KitName = record.KitName,
            Manufacturer = record.Manufacturer,
            InvoiceNo = record.InvoiceNo,
            BatchNo = record.BatchNo,
            DateOfManufacture = record.DateOfManufacture,
            DateOfExpiry = record.DateOfExpiry,
            QuantityReceived = record.QuantityReceived,
            OpeningBalance = record.OpeningBalance,
            NoOfTests = record.NoOfTests,
            ControlsUsed = record.ControlsUsed,
            Wastage = record.Wastage,
            ClosingBalance = record.ClosingBalance,
            TechSign = record.TechSign,
            MedicalOfficerSign = record.MedicalOfficerSign,
            Remarks = record.Remarks,
            CreatedAt = record.CreatedAt
        };
    }

    public async Task<IEnumerable<DiagnosticKitRegisterDto>> GetDiagnosticKitRegistersAsync()
    {
        var records = await _repo.GetDiagnosticKitRegistersAsync();
        return records.Select(MapKit);
    }

    public async Task<DiagnosticKitRegisterDto> CreateDiagnosticKitRegisterAsync(CreateDiagnosticKitRegisterDto d)
    {
        var existing = await _repo.GetDiagnosticKitRegistersAsync();
        var nextSl = existing.Count + 1;
        var closing = d.OpeningBalance + d.QuantityReceived - d.NoOfTests - d.ControlsUsed - d.Wastage;

        var item = new DiagnosticKitRegister
        {
            Id = Guid.NewGuid(),
            SlNo = d.SlNo > 0 ? d.SlNo : nextSl,
            Date = d.Date == default ? DateTime.UtcNow : d.Date,
            SupplierName = d.SupplierName.Trim(),
            KitName = d.KitName.Trim(),
            Manufacturer = d.Manufacturer.Trim(),
            InvoiceNo = d.InvoiceNo.Trim(),
            BatchNo = d.BatchNo.Trim(),
            DateOfManufacture = d.DateOfManufacture,
            DateOfExpiry = d.DateOfExpiry,
            QuantityReceived = d.QuantityReceived,
            OpeningBalance = d.OpeningBalance,
            NoOfTests = d.NoOfTests,
            ControlsUsed = d.ControlsUsed,
            Wastage = d.Wastage,
            ClosingBalance = closing,
            TechSign = d.TechSign?.Trim(),
            MedicalOfficerSign = d.MedicalOfficerSign?.Trim(),
            Remarks = d.Remarks?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddDiagnosticKitRegisterAsync(item);
        await _repo.SaveChangesAsync();
        return MapKit(item);
    }

    public async Task<DiagnosticKitRegisterDto?> UpdateDiagnosticKitRegisterAsync(Guid id, UpdateDiagnosticKitRegisterDto d)
    {
        var item = await _repo.FindDiagnosticKitRegisterAsync(id);
        if (item == null) return null;

        item.SupplierName = d.SupplierName.Trim();
        item.KitName = d.KitName.Trim();
        item.Manufacturer = d.Manufacturer.Trim();
        item.InvoiceNo = d.InvoiceNo.Trim();
        item.BatchNo = d.BatchNo.Trim();
        item.DateOfManufacture = d.DateOfManufacture;
        item.DateOfExpiry = d.DateOfExpiry;
        item.QuantityReceived = d.QuantityReceived;
        item.OpeningBalance = d.OpeningBalance;
        item.NoOfTests = d.NoOfTests;
        item.ControlsUsed = d.ControlsUsed;
        item.Wastage = d.Wastage;
        item.ClosingBalance = d.OpeningBalance + d.QuantityReceived - d.NoOfTests - d.ControlsUsed - d.Wastage;
        item.TechSign = d.TechSign?.Trim();
        item.MedicalOfficerSign = d.MedicalOfficerSign?.Trim();
        item.Remarks = d.Remarks?.Trim();

        await _repo.UpdateDiagnosticKitRegisterAsync(item);
        await _repo.SaveChangesAsync();
        return MapKit(item);
    }

    public async Task<bool> DeleteDiagnosticKitRegisterAsync(Guid id)
    {
        var item = await _repo.FindDiagnosticKitRegisterAsync(id);
        if (item == null) return false;
        _repo.RemoveDiagnosticKitRegister(item);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ==========================================
    // Uddanam Register 5: Apheresis / SDP Records
    // ==========================================
    private static ApheresisRecordDto MapApheresis(ApheresisRecord record)
    {
        return new ApheresisRecordDto
        {
            Id = record.Id,
            RegistrationNo = record.RegistrationNo,
            Date = record.Date,
            DonorName = record.DonorName,
            AgeSex = record.AgeSex,
            WeightKg = record.WeightKg,
            BloodGroup = record.BloodGroup,
            Phone = record.Phone,
            Email = record.Email,
            TimeOfMeal = record.TimeOfMeal,
            PatientName = record.PatientName,
            PatientIpNo = record.PatientIpNo,
            PatientBedNo = record.PatientBedNo,
            PatientBloodGroup = record.PatientBloodGroup,
            TakenAspirinLast3Days = record.TakenAspirinLast3Days,
            HaemoglobinGms = record.HaemoglobinGms,
            PlateletCountLakhs = record.PlateletCountLakhs,
            WbcCount = record.WbcCount,
            BloodPressure = record.BloodPressure,
            Pulse = record.Pulse,
            TemperatureCelsius = record.TemperatureCelsius,
            TotalProteinsGms = record.TotalProteinsGms,
            ProcedureStartTime = record.ProcedureStartTime,
            ProcedureEndTime = record.ProcedureEndTime,
            DurationMinutes = record.DurationMinutes,
            VenipunctureSite = record.VenipunctureSite,
            WholeBloodPumpRateMlMin = record.WholeBloodPumpRateMlMin,
            AcdRatio = record.AcdRatio,
            TotalBloodVolumeProcessedMl = record.TotalBloodVolumeProcessedMl,
            TotalProductCollectedMl = record.TotalProductCollectedMl,
            TotalVolumeAcdUsedMl = record.TotalVolumeAcdUsedMl,
            AdverseReaction = record.AdverseReaction,
            QualityControlPassed = record.QualityControlPassed,
            PhlebotomistSignature = record.PhlebotomistSignature,
            MedicalOfficerSignature = record.MedicalOfficerSignature,
            Remarks = record.Remarks,
            CreatedAt = record.CreatedAt,
            YearlySlNo = record.YearlySlNo,
            MonthlySlNo = record.MonthlySlNo
        };
    }

    public async Task<IEnumerable<ApheresisRecordDto>> GetApheresisRecordsAsync()
    {
        var records = await _repo.GetApheresisRecordsAsync();
        return records.Select(MapApheresis);
    }

    private async Task<string?> ValidateApheresisAsync(
        DateTime date,
        string donorName,
        string registrationNo,
        string? ageSex,
        decimal weightKg,
        decimal haemoglobin,
        decimal plateletLakhs,
        Guid? excludeId = null)
    {
        var ageMatch = System.Text.RegularExpressions.Regex.Match(ageSex ?? "", @"\d+");
        var age = ageMatch.Success ? int.Parse(ageMatch.Value) : 0;
        if (age is < 18 or > 60)
            return "Apheresis donor must be between 18 and 60 years old.";
        if (weightKg < 45m)
            return "Apheresis donor weight must be at least 45 kg.";
        if (haemoglobin < 12.5m)
            return "Apheresis donor hemoglobin must be at least 12.5 g/dL.";
        if (plateletLakhs < 2m)
            return "Apheresis platelet count must be at least 2.0 lakhs/cu mm.";

        var records = await _repo.GetApheresisRecordsAsync();
        var previous = records
            .Where(item => (!excludeId.HasValue || item.Id != excludeId.Value)
                && (string.Equals(item.RegistrationNo, registrationNo.Trim(), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.DonorName, donorName.Trim(), StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(item => item.Date)
            .FirstOrDefault();
        if (previous != null && date - previous.Date < TimeSpan.FromHours(48))
            return "Apheresis donor must wait at least 48 hours between procedures.";
        return null;
    }

    public async Task<ApheresisRecordDto> CreateApheresisRecordAsync(CreateApheresisRecordDto d)
    {
        var validationError = await ValidateApheresisAsync(d.Date, d.DonorName, d.RegistrationNo, d.AgeSex,
            d.WeightKg, d.HaemoglobinGms, d.PlateletCountLakhs);
        if (validationError != null)
            throw new InvalidOperationException(validationError);

        var existing = await _repo.GetApheresisRecordsAsync();
        var monthlyCount = existing.Count(item => item.Date.Year == d.Date.Year && item.Date.Month == d.Date.Month);
        var yearlyCount = existing.Count(item => item.Date.Year == d.Date.Year);
        var item = new ApheresisRecord
        {
            Id = Guid.NewGuid(),
            RegistrationNo = d.RegistrationNo.Trim(),
            Date = d.Date == default ? DateTime.UtcNow : d.Date,
            DonorName = d.DonorName.Trim(),
            AgeSex = d.AgeSex?.Trim(),
            WeightKg = d.WeightKg,
            BloodGroup = d.BloodGroup.Trim().ToUpperInvariant(),
            Phone = d.Phone?.Trim(),
            Email = d.Email?.Trim(),
            TimeOfMeal = d.TimeOfMeal?.Trim(),
            PatientName = d.PatientName?.Trim(),
            PatientIpNo = d.PatientIpNo?.Trim(),
            PatientBedNo = d.PatientBedNo?.Trim(),
            PatientBloodGroup = d.PatientBloodGroup?.Trim().ToUpperInvariant(),
            TakenAspirinLast3Days = d.TakenAspirinLast3Days,
            HaemoglobinGms = d.HaemoglobinGms,
            PlateletCountLakhs = d.PlateletCountLakhs,
            WbcCount = d.WbcCount,
            BloodPressure = d.BloodPressure?.Trim(),
            Pulse = d.Pulse,
            TemperatureCelsius = d.TemperatureCelsius,
            TotalProteinsGms = d.TotalProteinsGms,
            ProcedureStartTime = d.ProcedureStartTime?.Trim(),
            ProcedureEndTime = d.ProcedureEndTime?.Trim(),
            DurationMinutes = d.DurationMinutes,
            VenipunctureSite = d.VenipunctureSite.Trim(),
            WholeBloodPumpRateMlMin = d.WholeBloodPumpRateMlMin,
            AcdRatio = d.AcdRatio.Trim(),
            TotalBloodVolumeProcessedMl = d.TotalBloodVolumeProcessedMl,
            TotalProductCollectedMl = d.TotalProductCollectedMl,
            TotalVolumeAcdUsedMl = d.TotalVolumeAcdUsedMl,
            AdverseReaction = d.AdverseReaction?.Trim(),
            QualityControlPassed = d.QualityControlPassed,
            PhlebotomistSignature = d.PhlebotomistSignature?.Trim(),
            MedicalOfficerSignature = d.MedicalOfficerSignature?.Trim(),
            Remarks = d.Remarks?.Trim(),
            CreatedAt = DateTime.UtcNow,
            YearlySlNo = yearlyCount + 1,
            MonthlySlNo = monthlyCount + 1
        };

        await _repo.AddApheresisRecordAsync(item);
        await _repo.SaveChangesAsync();
        return MapApheresis(item);
    }

    public async Task<ApheresisRecordDto?> UpdateApheresisRecordAsync(Guid id, UpdateApheresisRecordDto d)
    {
        var item = await _repo.FindApheresisRecordAsync(id);
        if (item == null) return null;

        var validationError = await ValidateApheresisAsync(d.Date, d.DonorName, d.RegistrationNo, d.AgeSex,
            d.WeightKg, d.HaemoglobinGms, d.PlateletCountLakhs, id);
        if (validationError != null)
            throw new InvalidOperationException(validationError);

        item.RegistrationNo = d.RegistrationNo.Trim();
        item.DonorName = d.DonorName.Trim();
        item.AgeSex = d.AgeSex?.Trim();
        item.WeightKg = d.WeightKg;
        item.BloodGroup = d.BloodGroup.Trim().ToUpperInvariant();
        item.Phone = d.Phone?.Trim();
        item.Email = d.Email?.Trim();
        item.TimeOfMeal = d.TimeOfMeal?.Trim();
        item.PatientName = d.PatientName?.Trim();
        item.PatientIpNo = d.PatientIpNo?.Trim();
        item.PatientBedNo = d.PatientBedNo?.Trim();
        item.PatientBloodGroup = d.PatientBloodGroup?.Trim().ToUpperInvariant();
        item.TakenAspirinLast3Days = d.TakenAspirinLast3Days;
        item.HaemoglobinGms = d.HaemoglobinGms;
        item.PlateletCountLakhs = d.PlateletCountLakhs;
        item.WbcCount = d.WbcCount;
        item.BloodPressure = d.BloodPressure?.Trim();
        item.Pulse = d.Pulse;
        item.TemperatureCelsius = d.TemperatureCelsius;
        item.TotalProteinsGms = d.TotalProteinsGms;
        item.ProcedureStartTime = d.ProcedureStartTime?.Trim();
        item.ProcedureEndTime = d.ProcedureEndTime?.Trim();
        item.DurationMinutes = d.DurationMinutes;
        item.VenipunctureSite = d.VenipunctureSite.Trim();
        item.WholeBloodPumpRateMlMin = d.WholeBloodPumpRateMlMin;
        item.AcdRatio = d.AcdRatio.Trim();
        item.TotalBloodVolumeProcessedMl = d.TotalBloodVolumeProcessedMl;
        item.TotalProductCollectedMl = d.TotalProductCollectedMl;
        item.TotalVolumeAcdUsedMl = d.TotalVolumeAcdUsedMl;
        item.AdverseReaction = d.AdverseReaction?.Trim();
        item.QualityControlPassed = d.QualityControlPassed;
        item.PhlebotomistSignature = d.PhlebotomistSignature?.Trim();
        item.MedicalOfficerSignature = d.MedicalOfficerSignature?.Trim();
        item.Remarks = d.Remarks?.Trim();

        await _repo.UpdateApheresisRecordAsync(item);
        await _repo.SaveChangesAsync();
        return MapApheresis(item);
    }

    public async Task<bool> DeleteApheresisRecordAsync(Guid id)
    {
        var item = await _repo.FindApheresisRecordAsync(id);
        if (item == null) return false;
        _repo.RemoveApheresisRecord(item);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ==========================================
    // Master Record (UDDANAM MASTER RECORD 2026)
    // ==========================================
    public async Task<IEnumerable<MasterRecordItemDto>> GetMasterRecordsAsync()
    {
        var collections = await _repo.GetComponentCollectionsAsync();
        var ttiList = await _repo.GetTtiScreeningsAsync();
        var discards = await _repo.GetComponentDiscardsAsync();
        var fulfilledRequests = await _repo.GetRequestsAsync(null, "Fulfilled");

        var masterList = new List<MasterRecordItemDto>();
        int sl = 1;

        foreach (var col in collections)
        {
            var tti = ttiList.FirstOrDefault(t => t.BagBarcode.Equals(col.BagBarcode, StringComparison.OrdinalIgnoreCase));
            var discard = discards.FirstOrDefault(d => d.BagBarcode.Equals(col.BagBarcode, StringComparison.OrdinalIgnoreCase));
            var req = fulfilledRequests.FirstOrDefault(r => r.IssuedBagNumbers != null && r.IssuedBagNumbers.Contains(col.BagBarcode));

            masterList.Add(new MasterRecordItemDto
            {
                Id = col.Id,
                YearlySlNo = sl++,
                BagNumber = col.BagBarcode,
                BloodGroup = col.BloodGroup,
                ComponentType = col.ComponentType,
                DateOfCollection = col.CollectionDate,
                DateOfExpiry = col.ExpiryDate,
                BagType = col.BagType,
                VolumeMl = col.VolumeMl,
                DonorName = "Voluntary / Registered Donor",
                HivResult = tti?.HivResult ?? "Non-Reactive",
                HbsAgResult = tti?.HbsAgResult ?? "Non-Reactive",
                HcvResult = tti?.HcvResult ?? "Non-Reactive",
                VdrlResult = tti?.VdrlResult ?? "Non-Reactive",
                MalariaResult = tti?.MalariaResult ?? "Negative",
                AtypicalAntibodies = tti?.AtypicalAntibodies ?? "Negative",
                TtiOverallStatus = tti?.OverallStatus ?? "Passed",
                Status = col.Status,
                UtilisationIssueNo = req != null ? $"ISSUE-{req.Id.ToString()[..6].ToUpper()}" : null,
                DiscardReason = discard?.Reason,
                TechSign = tti?.TestedBy ?? "Lab Tech",
                MoSign = "Medical Officer"
            });
        }

        return masterList;
    }
}
