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
    public async Task<(BloodRequestResponseDto?,string?)> ProcessRequestAsync(Guid id,ProcessRequestDto d){var r=await _repo.FindRequestAsync(id);if(r==null)return(null,"Request not found.");if(d.Status=="Fulfilled"&&r.Status!="Fulfilled"){if(d.PaymentMode is not ("Cash" or "UPI"))return(null,"Select Cash or UPI payment before fulfilling the request.");if(d.PaymentMode=="UPI"&&string.IsNullOrWhiteSpace(d.TransactionId))return(null,"UPI transaction ID is required before completing payment.");var s=await _repo.FindStockAsync(r.BloodGroup,r.ComponentType);if(s==null||s.QuantityUnits<r.QuantityUnits)return(null,$"Insufficient stock of {r.BloodGroup} ({r.ComponentType}). Available: {s?.QuantityUnits??0} units, Requested: {r.QuantityUnits} units.");var bags=await _repo.GetAvailableBagsAsync(r.BloodGroup,r.ComponentType,r.QuantityUnits);if(bags.Count<r.QuantityUnits)return(null,$"Insufficient individual bags for {r.BloodGroup} ({r.ComponentType}). Available bags: {bags.Count}, Requested: {r.QuantityUnits}.");foreach(var b in bags){b.IsIssued=true;b.IssuedDate=DateTime.UtcNow;}r.IssuedBagNumbers=string.Join(", ",bags.Select(b=>b.BagNumber));s.QuantityUnits-=r.QuantityUnits;s.LastUpdated=DateTime.UtcNow;r.PaymentMode=d.PaymentMode;r.PaymentStatus="Completed";r.TransactionId=d.PaymentMode=="UPI"?d.TransactionId!.Trim():null;r.AmountPaid=d.AmountPaid??(GetDefaultComponentPrice(r.ComponentType)*r.QuantityUnits);}r.Status=d.Status;r.AdminComments=d.AdminComments;r.ProcessedAt=DateTime.UtcNow;await _repo.SaveChangesAsync();return(Mapping.Request(r),null);}
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
}
