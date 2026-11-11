using BloodBank.API.DTOs;
using BloodBank.API.Models;
using BloodBank.API.Repositories;

namespace BloodBank.API.Services;
internal static class Mapping
{
    public static BloodRequestResponseDto Request(BloodRequest r) => new() { Id=r.Id, RequesterId=r.RequesterId, RequesterType=r.RequesterType, RequesterName=r.RequesterName, RequesterContact=r.RequesterContact, HospitalName=r.HospitalName, PatientName=r.PatientName, BloodGroup=r.BloodGroup, ComponentType=r.ComponentType, QuantityUnits=r.QuantityUnits, Urgency=r.Urgency, Status=r.Status, HospitalDepartment=r.HospitalDepartment, MedicalReason=r.MedicalReason, AdminComments=r.AdminComments, CreatedAt=r.CreatedAt, ProcessedAt=r.ProcessedAt, IssuedBagNumbers=r.IssuedBagNumbers, PaymentMode=r.PaymentMode, PaymentStatus=r.PaymentStatus, TransactionId=r.TransactionId, AmountPaid=r.AmountPaid };
}
public sealed class BloodBankService : IBloodBankService
{
    private readonly IBloodBankRepository _repo;
    public BloodBankService(IBloodBankRepository repo) => _repo = repo;
    public async Task<IEnumerable<BloodStockDto>> GetStockAsync(string? g,string? c) => (await _repo.GetStocksAsync(string.IsNullOrWhiteSpace(g)?null:g.Trim().ToUpperInvariant(), string.IsNullOrWhiteSpace(c)?null:c.Trim())).Select(s=>new BloodStockDto {Id=s.Id,BloodGroup=s.BloodGroup,ComponentType=s.ComponentType,QuantityUnits=s.QuantityUnits,BloodBankName=s.BloodBankName,LastUpdated=s.LastUpdated});
    public async Task<BloodStockDto?> AddStockAsync(AddStockDto d) { var g=d.BloodGroup.Trim().ToUpperInvariant(); var c=d.ComponentType.Trim(); var n=d.BagNumber.Trim(); if(await _repo.BagNumberExistsAsync(n)) return null; await _repo.AddBagAsync(new BloodBag {BagNumber=n,BloodGroup=g,ComponentType=c,AddedDate=d.AddedDate==default?DateTime.UtcNow:d.AddedDate}); var s=await _repo.FindStockAsync(g,c); if(s==null){s=new BloodStock{Id=Guid.NewGuid(),BloodGroup=g,ComponentType=c,QuantityUnits=1,BloodBankName="Blood Bank A",LastUpdated=DateTime.UtcNow};await _repo.AddStockAsync(s);}else{s.QuantityUnits++;s.LastUpdated=DateTime.UtcNow;} await _repo.SaveChangesAsync(); return new BloodStockDto{Id=s.Id,BloodGroup=s.BloodGroup,ComponentType=s.ComponentType,QuantityUnits=s.QuantityUnits,BloodBankName=s.BloodBankName,LastUpdated=s.LastUpdated}; }
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
                AddedDate = DateTime.UtcNow
            });
        }

        await _repo.SaveChangesAsync();
        return new BloodStockDto { Id = s.Id, BloodGroup = s.BloodGroup, ComponentType = s.ComponentType, QuantityUnits = s.QuantityUnits, BloodBankName = s.BloodBankName, LastUpdated = s.LastUpdated };
    }
    public Task ClearAllDataAsync() => _repo.ClearOperationalDataAsync();
    public Task<bool> BagExistsAsync(string n)=>_repo.BagNumberExistsAsync(n);
    public async Task<IEnumerable<BloodBagDto>> GetBagsAsync(string? c)=> (await _repo.GetBagsAsync(string.IsNullOrWhiteSpace(c)?null:c.Trim())).Select(b=>new BloodBagDto{Id=b.Id,BagNumber=b.BagNumber,BloodGroup=b.BloodGroup,ComponentType=b.ComponentType,QuantityUnits=b.QuantityUnits,AddedDate=b.AddedDate});
    public async Task<IEnumerable<DonationCamp>> GetCampsAsync()=>await _repo.GetCampsAsync();
    public Task<DonationCamp?> GetCampAsync(Guid id)=>_repo.FindCampAsync(id);
    public async Task<DonationCamp> CreateCampAsync(CreateCampDto d){var c=new DonationCamp{Id=Guid.NewGuid(),CampName=d.CampName.Trim(),Location=d.Location.Trim(),CampDate=d.CampDate,TargetUnits=d.TargetUnits,OrganizedBy="Blood Bank A",ContactPhone=d.ContactPhone?.Trim()??"",Notes=d.Notes,Status="Scheduled",CreatedAt=DateTime.UtcNow};await _repo.AddCampAsync(c);await _repo.SaveChangesAsync();return c;}
    public async Task<DonationCamp?> UpdateCampAsync(Guid id,CreateCampDto d){var c=await _repo.FindCampAsync(id);if(c==null)return null;c.CampName=d.CampName.Trim();c.Location=d.Location.Trim();c.CampDate=d.CampDate;c.TargetUnits=d.TargetUnits;c.ContactPhone=d.ContactPhone?.Trim()??"";c.Notes=d.Notes;await _repo.SaveChangesAsync();return c;}
    public async Task<bool> DeleteCampAsync(Guid id){var c=await _repo.FindCampAsync(id);if(c==null)return false;_repo.RemoveCamp(c);await _repo.SaveChangesAsync();return true;}
    public async Task<IEnumerable<DonorDetailsDto>> GetDonorsAsync(){var ds=await _repo.GetDonorsAsync();var rs=await _repo.GetDonationsAsync(ds.Select(d=>d.Id));return ds.Select(d=>{var x=rs.Where(r=>r.DonorId==d.Id).OrderByDescending(r=>r.DonationDate).ToList();return new DonorDetailsDto{Id=d.Id,FullName=d.FullName,Email=d.Email,Phone=d.Phone,DateOfBirth=d.DateOfBirth,Sex=d.Sex,Address=d.Address,BloodGroup=d.BloodGroup,RegisteredDate=d.CreatedAt,TotalDonations=x.Count,LastDonationDate=x.FirstOrDefault()?.DonationDate};});}
    public async Task<CreateDonorResponseDto?> CreateDonorAsync(CreateDonorDto d)
    {
        var email = d.Email.Trim().ToLowerInvariant();
        if (await _repo.FindUserByEmailAsync(email) != null)
            return null;
        var fullName = $"{d.FirstName.Trim()} {d.LastName.Trim()}";
        var lastName = d.LastName.Trim().ToUpperInvariant();
        var generatedPassword = $"{lastName}@{d.DateOfBirth!.Value.Year}";
        var donor = new User
        {
            Id = Guid.NewGuid(), FullName = fullName, Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword, workFactor: 11),
            Role = "Donor", Phone = d.Phone.Trim(),
            DateOfBirth = d.DateOfBirth!.Value, Sex = d.Sex.Trim(), Address = d.Address.Trim(),
            BloodGroup = d.BloodGroup.Trim().ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        };
        await _repo.AddUserAsync(donor);
        await _repo.SaveChangesAsync();
        var donorDetails = new DonorDetailsDto
        {
            Id = donor.Id, FullName = donor.FullName, Email = donor.Email,
            Phone = donor.Phone, DateOfBirth = donor.DateOfBirth, Sex = donor.Sex,
            Address = donor.Address, BloodGroup = donor.BloodGroup,
            RegisteredDate = donor.CreatedAt, TotalDonations = 0
        };
        return new CreateDonorResponseDto { Donor = donorDetails, GeneratedPassword = generatedPassword };
    }

    public async Task<DonationCompletionResultDto> CompleteDonationAsync(Guid donorId, CompleteDonationDto d)
    {
        var donor = await _repo.FindUserAsync(donorId);
        if (donor == null || donor.Role != "Donor")
            return new DonationCompletionResultDto { Message = "Donor not found." };

        var donations = await _repo.GetDonationsAsync(new[] { donorId });
        var lastDonation = donations.OrderByDescending(x => x.DonationDate).FirstOrDefault();
        var age = donor.DateOfBirth.HasValue
            ? DateTime.UtcNow.Year - donor.DateOfBirth.Value.Year -
              (DateTime.UtcNow.Date < donor.DateOfBirth.Value.Date.AddYears(DateTime.UtcNow.Year - donor.DateOfBirth.Value.Year) ? 1 : 0)
            : 0;

        if (age < 18 || age > 65)
            return new DonationCompletionResultDto { Message = "Donor must be between 18 and 65 years old." };
        if (lastDonation != null && (DateTime.UtcNow - lastDonation.DonationDate).TotalDays < 90)
            return new DonationCompletionResultDto { Message = "Donor must wait 90 days between donations." };
        if (!d.FeelsHealthyToday)
            return new DonationCompletionResultDto { Message = "Donor must confirm they feel healthy today." };
        if (d.DonatedRecently || d.HadDonationDiscomfort || !d.AteRecently || !d.SleptWell ||
            d.HasInfectionRisk || d.HasColdFeverOrSinusitis || d.TookAntibioticsRecently ||
            d.TookAlcoholRecently || d.SmokesFrequently || d.TookMedicationRecently ||
            d.HadRecentVaccination || d.HadRecentIllness || d.HadRecentSurgeryOrTattoo ||
            d.HadRecentMalariaOrEndemicExposure || d.HasChronicDisease ||
            d.IsPregnantOrBreastfeeding || d.HadRecentAbortion ||
            d.HasUnsafeSexualExposure || d.HasHivOrHepatitisRisk)
            return new DonationCompletionResultDto { Message = "Donor has a temporary deferral and cannot donate yet." };
        if (d.HasPermanentDisqualification)
            return new DonationCompletionResultDto { Message = "Donor has a permanent disqualification." };
        if (!d.DonorConfirmed)
            return new DonationCompletionResultDto { Message = "Donor confirmation is required before completing the donation." };
        if (!d.MedicalExamAccepted)
            return new DonationCompletionResultDto { Message = "Medical examination must be accepted by blood-bank staff." };

        var donation = new DonationRecord
        {
            DonorId = donor.Id,
            DonorName = donor.FullName,
            BloodGroup = donor.BloodGroup ?? string.Empty,
            UnitsDonated = Math.Clamp(d.UnitsDonated, 1, 10),
            DonationDate = DateTime.UtcNow,
            CampName = d.CampName?.Trim(),
            WeightKg = d.WeightKg, PulsePerMinute = d.PulsePerMinute, Hemoglobin = d.Hemoglobin,
            Hematocrit = d.Hematocrit, PlateletCount = d.PlateletCount, BloodPressure = d.BloodPressure,
            TemperatureCelsius = d.TemperatureCelsius, ScreeningStaffName = d.ScreeningStaffName,
            ScreeningStaffSignature = d.ScreeningStaffSignature, ScreeningDate = d.ScreeningDate,
            ScreeningTime = d.ScreeningTime, MedicalExamAccepted = d.MedicalExamAccepted,
            DeferralReason = d.DeferralReason, MedicalOfficerName = d.MedicalOfficerName,
            MedicalOfficerSignature = d.MedicalOfficerSignature, MedicalOfficerDate = d.MedicalOfficerDate,
            DonorNumber = d.DonorNumber, BagType = d.BagType, SegmentNumber = d.SegmentNumber,
            DonationStartTime = d.DonationStartTime, DonationEndTime = d.DonationEndTime,
            VenipunctureSite = d.VenipunctureSite, PhlebotomistName = d.PhlebotomistName,
            PhlebotomistSignature = d.PhlebotomistSignature, VisualInspectionPassed = d.VisualInspectionPassed,
            DonorConfirmed = d.DonorConfirmed, FeelsHealthyToday = d.FeelsHealthyToday,
            DonatedRecently = d.DonatedRecently, HadDonationDiscomfort = d.HadDonationDiscomfort,
            AteRecently = d.AteRecently, SleptWell = d.SleptWell, HasInfectionRisk = d.HasInfectionRisk,
            HasColdFeverOrSinusitis = d.HasColdFeverOrSinusitis, TookAntibioticsRecently = d.TookAntibioticsRecently,
            TookAlcoholRecently = d.TookAlcoholRecently, SmokesFrequently = d.SmokesFrequently,
            TookMedicationRecently = d.TookMedicationRecently, HadRecentVaccination = d.HadRecentVaccination,
            HadRecentIllness = d.HadRecentIllness, HadRecentSurgeryOrTattoo = d.HadRecentSurgeryOrTattoo,
            HadRecentMalariaOrEndemicExposure = d.HadRecentMalariaOrEndemicExposure,
            HasChronicDisease = d.HasChronicDisease, IsPregnantOrBreastfeeding = d.IsPregnantOrBreastfeeding,
            HadRecentAbortion = d.HadRecentAbortion, HasUnsafeSexualExposure = d.HasUnsafeSexualExposure,
            HasHivOrHepatitisRisk = d.HasHivOrHepatitisRisk, HasPermanentDisqualification = d.HasPermanentDisqualification
        };
        await _repo.AddDonationAsync(donation);
        await _repo.SaveChangesAsync();

        var updatedDonor = (await GetDonorsAsync()).First(x => x.Id == donor.Id);
        return new DonationCompletionResultDto { IsEligible = true, Message = "Donation completed successfully.", Donor = updatedDonor };
    }
    public async Task<IEnumerable<DonationHistoryDto>> GetDonationHistoryAsync(Guid donorId)
    {
        return (await _repo.GetDonationsForDonorAsync(donorId)).Select(r => new DonationHistoryDto
        {
            Id = r.Id, DonorId = r.DonorId, DonorName = r.DonorName, BloodGroup = r.BloodGroup,
            UnitsDonated = r.UnitsDonated, DonationDate = r.DonationDate, CampName = r.CampName,
            BloodBankName = r.BloodBankName, WeightKg = r.WeightKg, PulsePerMinute = r.PulsePerMinute,
            Hemoglobin = r.Hemoglobin, Hematocrit = r.Hematocrit, PlateletCount = r.PlateletCount,
            BloodPressure = r.BloodPressure, TemperatureCelsius = r.TemperatureCelsius,
            ScreeningStaffName = r.ScreeningStaffName, ScreeningDate = r.ScreeningDate,
            ScreeningTime = r.ScreeningTime, MedicalExamAccepted = r.MedicalExamAccepted,
            MedicalOfficerName = r.MedicalOfficerName, MedicalOfficerDate = r.MedicalOfficerDate,
            DonorNumber = r.DonorNumber, BagType = r.BagType, SegmentNumber = r.SegmentNumber,
            DonationStartTime = r.DonationStartTime, DonationEndTime = r.DonationEndTime,
            VenipunctureSite = r.VenipunctureSite, PhlebotomistName = r.PhlebotomistName,
            VisualInspectionPassed = r.VisualInspectionPassed, DonorConfirmed = r.DonorConfirmed,
            FeelsHealthyToday = r.FeelsHealthyToday, DonatedRecently = r.DonatedRecently,
            HadDonationDiscomfort = r.HadDonationDiscomfort, AteRecently = r.AteRecently,
            SleptWell = r.SleptWell, HasInfectionRisk = r.HasInfectionRisk,
            HasColdFeverOrSinusitis = r.HasColdFeverOrSinusitis, TookAntibioticsRecently = r.TookAntibioticsRecently,
            TookAlcoholRecently = r.TookAlcoholRecently, SmokesFrequently = r.SmokesFrequently,
            TookMedicationRecently = r.TookMedicationRecently, HadRecentVaccination = r.HadRecentVaccination,
            HadRecentIllness = r.HadRecentIllness, HadRecentSurgeryOrTattoo = r.HadRecentSurgeryOrTattoo,
            HadRecentMalariaOrEndemicExposure = r.HadRecentMalariaOrEndemicExposure,
            HasChronicDisease = r.HasChronicDisease, IsPregnantOrBreastfeeding = r.IsPregnantOrBreastfeeding,
            HadRecentAbortion = r.HadRecentAbortion, HasUnsafeSexualExposure = r.HasUnsafeSexualExposure,
            HasHivOrHepatitisRisk = r.HasHivOrHepatitisRisk, HasPermanentDisqualification = r.HasPermanentDisqualification
        });
    }
    public async Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(string? s)=>(await _repo.GetRequestsAsync(status:string.IsNullOrWhiteSpace(s)?null:s.Trim())).Select(Mapping.Request);
    public async Task<BloodRequestResponseDto> CreateRequestAsync(Guid id,CreateBloodRequestDto d){var u=await _repo.FindUserAsync(id);var r=CreateRequest(id,"BloodBank",u?.FullName??"Blood Bank A",u?.Phone??u?.Email,d);await _repo.AddRequestAsync(r);await _repo.SaveChangesAsync();return Mapping.Request(r);}
    private static BloodRequest CreateRequest(Guid id,string type,string name,string? contact,CreateBloodRequestDto d)=>new(){Id=Guid.NewGuid(),RequesterId=id,RequesterType=type,RequesterName=name,RequesterContact=contact,HospitalName=d.HospitalName?.Trim(),PatientName=d.PatientName.Trim(),BloodGroup=d.BloodGroup.Trim().ToUpperInvariant(),ComponentType=d.ComponentType.Trim(),QuantityUnits=d.QuantityUnits,Urgency=d.Urgency,Status="Pending",HospitalDepartment=d.HospitalDepartment,MedicalReason=d.MedicalReason,CreatedAt=DateTime.UtcNow};
    public async Task<(BloodRequestResponseDto?, string?)> ProcessRequestAsync(Guid id, ProcessRequestDto d)
    {
        var r = await _repo.FindRequestAsync(id);
        if (r == null) return (null, "Request not found.");

        if (d.Status == "Fulfilled" && r.Status != "Fulfilled")
        {
            if (d.PaymentMode is not ("Cash" or "UPI"))
                return (null, "Select Cash or UPI payment before fulfilling the request.");
            if (d.PaymentMode == "UPI" && string.IsNullOrWhiteSpace(d.TransactionId))
                return (null, "UPI transaction ID is required before completing payment.");

            var s = await _repo.FindStockAsync(r.BloodGroup, r.ComponentType);
            if (s == null || s.QuantityUnits < r.QuantityUnits)
                return (null, $"Insufficient stock of {r.BloodGroup} ({r.ComponentType}). Available: {s?.QuantityUnits ?? 0} units, Requested: {r.QuantityUnits} units.");

            // Get existing available bags for this component and blood group
            var bags = await _repo.GetAvailableBagsAsync(r.BloodGroup, r.ComponentType, r.QuantityUnits);
            var issuedBagList = new List<string>();

            // Mark existing bags as Issued in both BloodBags and ComponentCollections
            foreach (var b in bags)
            {
                b.IsIssued = true;
                b.IssuedDate = DateTime.UtcNow;
                issuedBagList.Add(b.BagNumber);

                var collectionItem = await _repo.FindComponentCollectionByBarcodeAsync(b.BagNumber);
                if (collectionItem != null)
                {
                    collectionItem.Status = "Issued";
                }
            }

            // If more units were requested than pre-barcoded bags in DB, generate tracked issued bags
            for (int i = bags.Count; i < r.QuantityUnits; i++)
            {
                var prefix = r.ComponentType.Contains("Whole") ? "WB" :
                             r.ComponentType.Contains("PRBC") ? "PRBC" :
                             r.ComponentType.Contains("Platelet") ? "PLT" :
                             r.ComponentType.Contains("Plasma") ? "FFP" : "CRYO";
                var cleanGroup = r.BloodGroup.Replace("+", "P").Replace("-", "N");
                var autoBarcode = $"BAG-{prefix}-{cleanGroup}-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                await _repo.AddBagAsync(new BloodBag
                {
                    Id = Guid.NewGuid(),
                    BagNumber = autoBarcode,
                    BloodGroup = r.BloodGroup,
                    ComponentType = r.ComponentType,
                    QuantityUnits = 1,
                    AddedDate = DateTime.UtcNow,
                    IsIssued = true,
                    IssuedDate = DateTime.UtcNow
                });

                await _repo.AddComponentCollectionAsync(new ComponentCollection
                {
                    Id = Guid.NewGuid(),
                    BagBarcode = autoBarcode,
                    ComponentType = r.ComponentType,
                    BloodGroup = r.BloodGroup,
                    BagType = r.ComponentType.Contains("Platelet") ? "Single Bag (250ml)" : r.ComponentType.Contains("Whole") ? "Triple Bag (450ml)" : "Double Bag (350ml)",
                    VolumeMl = r.ComponentType.Contains("Platelet") ? 250 : r.ComponentType.Contains("Whole") ? 450 : 350,
                    CollectionDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(r.ComponentType.Contains("Platelet") ? 5 : r.ComponentType.Contains("Whole") ? 35 : 42),
                    Status = "Issued"
                });

                issuedBagList.Add(autoBarcode);
            }

            r.IssuedBagNumbers = string.Join(", ", issuedBagList);
            s.QuantityUnits = Math.Max(0, s.QuantityUnits - r.QuantityUnits);
            s.LastUpdated = DateTime.UtcNow;

            r.PaymentMode = d.PaymentMode;
            r.PaymentStatus = "Completed";
            r.TransactionId = d.PaymentMode == "UPI" ? d.TransactionId!.Trim() : null;
            r.AmountPaid = d.AmountPaid ?? (GetDefaultComponentPrice(r.ComponentType) * r.QuantityUnits);
        }

        r.Status = d.Status;
        r.AdminComments = d.AdminComments;
        r.ProcessedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();
        return (Mapping.Request(r), null);
    }
    private static decimal GetDefaultComponentPrice(string componentType) => componentType switch
    {
        "Whole Blood" => 1500m,
        "Red Blood Cells (PRBC)" => 1450m,
        "Platelets" => 850m,
        "Fresh Frozen Plasma (FFP)" => 600m,
        "Cryoprecipitate" => 500m,
        _ => 1000m
    };
    private static ComponentCollectionDto MapCollection(ComponentCollection x) => new() { Id=x.Id, ComponentType=x.ComponentType, BloodGroup=x.BloodGroup, BagType=x.BagType, VolumeMl=x.VolumeMl, BagBarcode=x.BagBarcode, CollectionDate=x.CollectionDate, ExpiryDate=x.ExpiryDate, Status=x.Status };
    private static ComponentDiscardDto MapDiscard(ComponentDiscard x) => new() { Id=x.Id, BagBarcode=x.BagBarcode, Reason=x.Reason, DiscardedBy=x.DiscardedBy, ApprovalStatus=x.ApprovalStatus, ApprovedBy=x.ApprovedBy, CreatedAt=x.CreatedAt, ApprovedAt=x.ApprovedAt };
    private static ComponentReturnDto MapReturn(ComponentReturn x) => new() { Id=x.Id, BagBarcode=x.BagBarcode, Reason=x.Reason, ReturnedBy=x.ReturnedBy, ReturnDate=x.ReturnDate };

    public async Task<IEnumerable<ComponentCollectionDto>> GetComponentCollectionsAsync() => (await _repo.GetComponentCollectionsAsync()).Select(MapCollection);
    public async Task<(ComponentCollectionDto? Result, string? Error)> CreateComponentCollectionAsync(CreateComponentCollectionDto d)
    {
        var barcode = d.BagBarcode.Trim();
        if (await _repo.FindComponentCollectionByBarcodeAsync(barcode) != null || await _repo.BagNumberExistsAsync(barcode))
            return (null, "A component with this bag barcode already exists.");
        if (d.ExpiryDate.Date <= d.CollectionDate.Date) return (null, "Expiry date must be after the collection date.");
        var item = new ComponentCollection { ComponentType=d.ComponentType.Trim(), BloodGroup=d.BloodGroup.Trim().ToUpperInvariant(), BagType=d.BagType.Trim(), VolumeMl=d.VolumeMl, BagBarcode=barcode, CollectionDate=d.CollectionDate, ExpiryDate=d.ExpiryDate, Status="Available" };
        await _repo.AddComponentCollectionAsync(item);
        await _repo.AddBagAsync(new BloodBag { BagNumber=barcode, BloodGroup=item.BloodGroup, ComponentType=item.ComponentType, QuantityUnits=1, AddedDate=item.CollectionDate });
        var stock = await _repo.FindStockAsync(item.BloodGroup, item.ComponentType);
        if (stock == null) { stock = new BloodStock { Id=Guid.NewGuid(), BloodGroup=item.BloodGroup, ComponentType=item.ComponentType, QuantityUnits=1, BloodBankName="Blood Bank A", LastUpdated=DateTime.UtcNow }; await _repo.AddStockAsync(stock); } else { stock.QuantityUnits++; stock.LastUpdated=DateTime.UtcNow; }
        await _repo.SaveChangesAsync();
        return (MapCollection(item), null);
    }
    public async Task<(ComponentCollectionDto? Result, string? Error)> UpdateComponentCollectionAsync(Guid id, CreateComponentCollectionDto d)
    {
        var item = await _repo.FindComponentCollectionAsync(id);
        if (item == null) return (null, "Component collection not found.");
        if (item.Status != "Available") return (null, "Only available components can be edited.");
        if (d.ExpiryDate.Date <= d.CollectionDate.Date) return (null, "Expiry date must be after the collection date.");
        var bag = await _repo.FindBagByNumberAsync(item.BagBarcode);
        item.ComponentType=d.ComponentType.Trim(); item.BloodGroup=d.BloodGroup.Trim().ToUpperInvariant(); item.BagType=d.BagType.Trim(); item.VolumeMl=d.VolumeMl; item.CollectionDate=d.CollectionDate; item.ExpiryDate=d.ExpiryDate;
        if (bag != null) { bag.ComponentType=item.ComponentType; bag.BloodGroup=item.BloodGroup; bag.AddedDate=item.CollectionDate; }
        await _repo.SaveChangesAsync();
        return (MapCollection(item), null);
    }
    public async Task<bool> DeleteComponentCollectionAsync(Guid id)
    {
        var item = await _repo.FindComponentCollectionAsync(id); if (item == null || item.Status != "Available") return false;
        var bag = await _repo.FindBagByNumberAsync(item.BagBarcode); if (bag?.IsIssued == true) return false;
        var stock = await _repo.FindStockAsync(item.BloodGroup, item.ComponentType); if (stock != null) { stock.QuantityUnits=Math.Max(0, stock.QuantityUnits-1); stock.LastUpdated=DateTime.UtcNow; }
        if (bag != null) _repo.RemoveBag(bag); _repo.RemoveComponentCollection(item); await _repo.SaveChangesAsync(); return true;
    }
    public async Task<IEnumerable<ComponentDiscardDto>> GetComponentDiscardsAsync() => (await _repo.GetComponentDiscardsAsync()).Select(MapDiscard);
    public async Task<(ComponentDiscardDto? Result, string? Error)> CreateComponentDiscardAsync(CreateComponentDiscardDto d)
    {
        var item = await _repo.FindComponentCollectionByBarcodeAsync(d.BagBarcode.Trim()); if (item == null) return (null, "Component barcode was not found.");
        if (item.Status != "Available") return (null, "Only available components can be submitted for discard.");
        var discard = new ComponentDiscard { BagBarcode=item.BagBarcode, Reason=d.Reason.Trim(), DiscardedBy=d.DiscardedBy.Trim() };
        await _repo.AddComponentDiscardAsync(discard); await _repo.SaveChangesAsync(); return (MapDiscard(discard), null);
    }
    public async Task<(ComponentDiscardDto? Result, string? Error)> ReviewComponentDiscardAsync(Guid id, bool approve, string approvedBy)
    {
        var discard = await _repo.FindComponentDiscardAsync(id); if (discard == null) return (null, "Discard request not found.");
        if (discard.ApprovalStatus != "Pending") return (null, "This discard request has already been reviewed.");
        discard.ApprovalStatus = approve ? "Approved" : "Rejected"; discard.ApprovedBy=approvedBy; discard.ApprovedAt=DateTime.UtcNow;
        if (approve) { var item=await _repo.FindComponentCollectionByBarcodeAsync(discard.BagBarcode); if (item != null) { item.Status="Discarded"; var stock=await _repo.FindStockAsync(item.BloodGroup,item.ComponentType); if(stock!=null){stock.QuantityUnits=Math.Max(0,stock.QuantityUnits-1);stock.LastUpdated=DateTime.UtcNow;} } }
        await _repo.SaveChangesAsync(); return (MapDiscard(discard), null);
    }
    public async Task<bool> DeleteComponentDiscardAsync(Guid id) { var item=await _repo.FindComponentDiscardAsync(id); if(item==null)return false; _repo.RemoveComponentDiscard(item); await _repo.SaveChangesAsync(); return true; }
    public async Task<IEnumerable<ComponentReturnDto>> GetComponentReturnsAsync() => (await _repo.GetComponentReturnsAsync()).Select(MapReturn);
    public async Task<(ComponentReturnDto? Result, string? Error)> CreateComponentReturnAsync(CreateComponentReturnDto d)
    {
        var barcode=d.BagBarcode.Trim(); var bag=await _repo.FindBagByNumberAsync(barcode); if(bag==null)return(null,"Issued component barcode was not found."); if(!bag.IsIssued)return(null,"This component is not currently issued to a patient.");
        bag.IsIssued=false; bag.IssuedDate=null; var item=await _repo.FindComponentCollectionByBarcodeAsync(barcode); if(item!=null)item.Status="Available";
        var stock=await _repo.FindStockAsync(bag.BloodGroup,bag.ComponentType); if(stock==null){stock=new BloodStock{Id=Guid.NewGuid(),BloodGroup=bag.BloodGroup,ComponentType=bag.ComponentType,QuantityUnits=1,BloodBankName="Blood Bank A",LastUpdated=DateTime.UtcNow};await _repo.AddStockAsync(stock);}else{stock.QuantityUnits++;stock.LastUpdated=DateTime.UtcNow;}
        var returned=new ComponentReturn{BagBarcode=barcode,Reason=d.Reason.Trim(),ReturnedBy=d.ReturnedBy.Trim()}; await _repo.AddComponentReturnAsync(returned); await _repo.SaveChangesAsync(); return(MapReturn(returned),null);
    }
    public async Task<bool> DeleteComponentReturnAsync(Guid id) { var item=await _repo.FindComponentReturnAsync(id); if(item==null)return false; _repo.RemoveComponentReturn(item); await _repo.SaveChangesAsync(); return true; }

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
        var donor = await _repo.FindUserAsync(id);
        if (donor == null || donor.Role != "Donor") return null;

        var email = d.Email.Trim().ToLowerInvariant();
        var existing = await _repo.FindUserByEmailAsync(email);
        if (existing != null && existing.Id != id)
            return null;

        donor.FullName = $"{d.FirstName.Trim()} {d.LastName.Trim()}";
        donor.Email = email;
        donor.Phone = d.Phone.Trim();
        donor.DateOfBirth = d.DateOfBirth;
        donor.Sex = d.Sex?.Trim();
        donor.Address = d.Address?.Trim();
        donor.BloodGroup = d.BloodGroup.Trim().ToUpperInvariant();

        await _repo.SaveChangesAsync();

        var donations = (await _repo.GetDonationsAsync(new[] { donor.Id })).ToList();
        return new DonorDetailsDto
        {
            Id = donor.Id,
            FullName = donor.FullName,
            Email = donor.Email,
            Phone = donor.Phone,
            DateOfBirth = donor.DateOfBirth,
            Sex = donor.Sex,
            Address = donor.Address,
            BloodGroup = donor.BloodGroup,
            RegisteredDate = donor.CreatedAt,
            TotalDonations = donations.Count,
            LastDonationDate = donations.OrderByDescending(r => r.DonationDate).FirstOrDefault()?.DonationDate
        };
    }

    public async Task<BloodRequestResponseDto?> UpdateRequestAsync(Guid id, UpdateBloodRequestDto d)
    {
        var req = await _repo.FindRequestAsync(id);
        if (req == null) return null;

        if (!string.IsNullOrWhiteSpace(d.HospitalName)) req.HospitalName = d.HospitalName.Trim();
        if (!string.IsNullOrWhiteSpace(d.PatientName)) req.PatientName = d.PatientName.Trim();
        if (!string.IsNullOrWhiteSpace(d.BloodGroup)) req.BloodGroup = d.BloodGroup.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(d.ComponentType)) req.ComponentType = d.ComponentType.Trim();
        if (d.QuantityUnits > 0) req.QuantityUnits = d.QuantityUnits;
        if (!string.IsNullOrWhiteSpace(d.Urgency)) req.Urgency = d.Urgency.Trim();
        if (d.HospitalDepartment != null) req.HospitalDepartment = d.HospitalDepartment.Trim();
        if (d.MedicalReason != null) req.MedicalReason = d.MedicalReason.Trim();
        if (!string.IsNullOrWhiteSpace(d.Status) && req.Status != "Fulfilled") req.Status = d.Status.Trim();
        if (!string.IsNullOrWhiteSpace(d.AdminComments)) req.AdminComments = d.AdminComments.Trim();

        await _repo.SaveChangesAsync();
        return Mapping.Request(req);
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
    private static TtiScreeningDto MapTti(TtiScreeningRecord x) => new()
    {
        Id = x.Id,
        BagBarcode = x.BagBarcode,
        BloodGroup = x.BloodGroup,
        TestingDate = x.TestingDate,
        HivResult = x.HivResult,
        HbsAgResult = x.HbsAgResult,
        HcvResult = x.HcvResult,
        VdrlResult = x.VdrlResult,
        MalariaResult = x.MalariaResult,
        AtypicalAntibodies = x.AtypicalAntibodies,
        OverallStatus = x.OverallStatus,
        TestedBy = x.TestedBy,
        ApprovedBy = x.ApprovedBy,
        Remarks = x.Remarks,
        CreatedAt = x.CreatedAt
    };

    public async Task<IEnumerable<TtiScreeningDto>> GetTtiScreeningsAsync(string? status = null) =>
        (await _repo.GetTtiScreeningsAsync(status)).Select(MapTti);

    public async Task<TtiScreeningDto> CreateTtiScreeningAsync(CreateTtiScreeningDto d)
    {
        bool isReactive = d.HivResult.Contains("Reactive", StringComparison.OrdinalIgnoreCase)
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
            HbsAgResult = d.HbsAgResult.Trim(),
            HcvResult = d.HcvResult.Trim(),
            VdrlResult = d.VdrlResult.Trim(),
            MalariaResult = d.MalariaResult.Trim(),
            AtypicalAntibodies = d.AtypicalAntibodies.Trim(),
            OverallStatus = overallStatus,
            TestedBy = d.TestedBy.Trim(),
            ApprovedBy = d.ApprovedBy?.Trim(),
            Remarks = d.Remarks?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddTtiScreeningAsync(record);

        if (isReactive)
        {
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
        record.HbsAgResult = d.HbsAgResult.Trim();
        record.HcvResult = d.HcvResult.Trim();
        record.VdrlResult = d.VdrlResult.Trim();
        record.MalariaResult = d.MalariaResult.Trim();
        record.AtypicalAntibodies = d.AtypicalAntibodies.Trim();
        record.OverallStatus = d.OverallStatus.Trim();
        record.TestedBy = d.TestedBy.Trim();
        record.ApprovedBy = d.ApprovedBy?.Trim();
        record.Remarks = d.Remarks?.Trim();

        await _repo.UpdateTtiScreeningAsync(record);
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
    private static PatientGroupingDto MapGrouping(PatientGroupingRecord x) => new()
    {
        Id = x.Id,
        SlNo = x.SlNo,
        Date = x.Date,
        PatientName = x.PatientName,
        AgeSex = x.AgeSex,
        HospitalName = x.HospitalName,
        IpNumber = x.IpNumber,
        BagNumber = x.BagNumber,
        BagType = x.BagType,
        BloodGroup = x.BloodGroup,
        RhType = x.RhType,
        AntiA = x.AntiA,
        AntiB = x.AntiB,
        AntiAb = x.AntiAb,
        AntiD = x.AntiD,
        LectinA1 = x.LectinA1,
        LectinH = x.LectinH,
        ACells = x.ACells,
        BCells = x.BCells,
        OCells = x.OCells,
        DuTest = x.DuTest,
        IrregularAntibodies = x.IrregularAntibodies,
        AutoAntibodies = x.AutoAntibodies,
        CrossmatchSaline = x.CrossmatchSaline,
        CrossmatchAlbumin = x.CrossmatchAlbumin,
        CrossmatchAhg = x.CrossmatchAhg,
        TechnicianSignature = x.TechnicianSignature,
        MedicalOfficerSignature = x.MedicalOfficerSignature,
        CreatedAt = x.CreatedAt
    };

    public async Task<IEnumerable<PatientGroupingDto>> GetPatientGroupingsAsync() =>
        (await _repo.GetPatientGroupingsAsync()).Select(MapGrouping);

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

    public async Task<bool> DeletePatientGroupingAsync(Guid id)
    {
        var item = await _repo.FindPatientGroupingAsync(id);
        if (item == null) return false;
        _repo.RemovePatientGrouping(item);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ==========================================
    // Uddanam Register 3: CPDA Blood Bags Stock Register
    // ==========================================
    private static BloodBagStockRegisterDto MapBagStock(BloodBagStockRegister x) => new()
    {
        Id = x.Id,
        SlNo = x.SlNo,
        Date = x.Date,
        SupplierName = x.SupplierName,
        InvoiceNo = x.InvoiceNo,
        InvoiceDate = x.InvoiceDate,
        Manufacturer = x.Manufacturer,
        BatchNo = x.BatchNo,
        DateOfSupply = x.DateOfSupply,
        DateOfManufacture = x.DateOfManufacture,
        DateOfExpiry = x.DateOfExpiry,
        CodeSpecification = x.CodeSpecification,
        ResultCoa = x.ResultCoa,
        OpeningStock = x.OpeningStock,
        QuantityReceived = x.QuantityReceived,
        QuantityUsed = x.QuantityUsed,
        Wastage = x.Wastage,
        Balance = x.Balance,
        TechSign = x.TechSign,
        MedicalOfficerSign = x.MedicalOfficerSign,
        Remarks = x.Remarks,
        CreatedAt = x.CreatedAt
    };

    public async Task<IEnumerable<BloodBagStockRegisterDto>> GetBloodBagStockRegistersAsync() =>
        (await _repo.GetBloodBagStockRegistersAsync()).Select(MapBagStock);

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
    private static DiagnosticKitRegisterDto MapKit(DiagnosticKitRegister x) => new()
    {
        Id = x.Id,
        SlNo = x.SlNo,
        Date = x.Date,
        SupplierName = x.SupplierName,
        KitName = x.KitName,
        Manufacturer = x.Manufacturer,
        InvoiceNo = x.InvoiceNo,
        BatchNo = x.BatchNo,
        DateOfManufacture = x.DateOfManufacture,
        DateOfExpiry = x.DateOfExpiry,
        QuantityReceived = x.QuantityReceived,
        OpeningBalance = x.OpeningBalance,
        NoOfTests = x.NoOfTests,
        ControlsUsed = x.ControlsUsed,
        Wastage = x.Wastage,
        ClosingBalance = x.ClosingBalance,
        TechSign = x.TechSign,
        MedicalOfficerSign = x.MedicalOfficerSign,
        Remarks = x.Remarks,
        CreatedAt = x.CreatedAt
    };

    public async Task<IEnumerable<DiagnosticKitRegisterDto>> GetDiagnosticKitRegistersAsync() =>
        (await _repo.GetDiagnosticKitRegistersAsync()).Select(MapKit);

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
    private static ApheresisRecordDto MapApheresis(ApheresisRecord x) => new()
    {
        Id = x.Id,
        RegistrationNo = x.RegistrationNo,
        Date = x.Date,
        DonorName = x.DonorName,
        AgeSex = x.AgeSex,
        WeightKg = x.WeightKg,
        BloodGroup = x.BloodGroup,
        Phone = x.Phone,
        Email = x.Email,
        TimeOfMeal = x.TimeOfMeal,
        PatientName = x.PatientName,
        PatientIpNo = x.PatientIpNo,
        PatientBedNo = x.PatientBedNo,
        PatientBloodGroup = x.PatientBloodGroup,
        TakenAspirinLast3Days = x.TakenAspirinLast3Days,
        HaemoglobinGms = x.HaemoglobinGms,
        PlateletCountLakhs = x.PlateletCountLakhs,
        WbcCount = x.WbcCount,
        BloodPressure = x.BloodPressure,
        Pulse = x.Pulse,
        TemperatureCelsius = x.TemperatureCelsius,
        TotalProteinsGms = x.TotalProteinsGms,
        ProcedureStartTime = x.ProcedureStartTime,
        ProcedureEndTime = x.ProcedureEndTime,
        DurationMinutes = x.DurationMinutes,
        VenipunctureSite = x.VenipunctureSite,
        WholeBloodPumpRateMlMin = x.WholeBloodPumpRateMlMin,
        AcdRatio = x.AcdRatio,
        TotalBloodVolumeProcessedMl = x.TotalBloodVolumeProcessedMl,
        TotalProductCollectedMl = x.TotalProductCollectedMl,
        TotalVolumeAcdUsedMl = x.TotalVolumeAcdUsedMl,
        AdverseReaction = x.AdverseReaction,
        QualityControlPassed = x.QualityControlPassed,
        PhlebotomistSignature = x.PhlebotomistSignature,
        MedicalOfficerSignature = x.MedicalOfficerSignature,
        Remarks = x.Remarks,
        CreatedAt = x.CreatedAt
    };

    public async Task<IEnumerable<ApheresisRecordDto>> GetApheresisRecordsAsync() =>
        (await _repo.GetApheresisRecordsAsync()).Select(MapApheresis);

    public async Task<ApheresisRecordDto> CreateApheresisRecordAsync(CreateApheresisRecordDto d)
    {
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
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddApheresisRecordAsync(item);
        await _repo.SaveChangesAsync();
        return MapApheresis(item);
    }

    public async Task<ApheresisRecordDto?> UpdateApheresisRecordAsync(Guid id, UpdateApheresisRecordDto d)
    {
        var item = await _repo.FindApheresisRecordAsync(id);
        if (item == null) return null;

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
