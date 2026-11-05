using BloodBank.API.DTOs;
using BloodBank.API.Models;
using BloodBank.API.Repositories;

namespace BloodBank.API.Services;
internal static class Mapping
{
    public static BloodRequestResponseDto Request(BloodRequest r) => new() { Id=r.Id, RequesterId=r.RequesterId, RequesterType=r.RequesterType, RequesterName=r.RequesterName, RequesterContact=r.RequesterContact, HospitalName=r.HospitalName, PatientName=r.PatientName, BloodGroup=r.BloodGroup, ComponentType=r.ComponentType, QuantityUnits=r.QuantityUnits, Urgency=r.Urgency, Status=r.Status, HospitalDepartment=r.HospitalDepartment, MedicalReason=r.MedicalReason, AdminComments=r.AdminComments, CreatedAt=r.CreatedAt, ProcessedAt=r.ProcessedAt, IssuedBagNumbers=r.IssuedBagNumbers };
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
    public async Task<IEnumerable<DonorDetailsDto>> GetDonorsAsync(){var ds=await _repo.GetDonorsAsync();var rs=await _repo.GetDonationsAsync(ds.Select(d=>d.Id));return ds.Select(d=>{var x=rs.Where(r=>r.DonorId==d.Id).OrderByDescending(r=>r.DonationDate).ToList();return new DonorDetailsDto{Id=d.Id,FullName=d.FullName,Email=d.Email,Phone=d.Phone,BloodGroup=d.BloodGroup,RegisteredDate=d.CreatedAt,TotalDonations=x.Count,LastDonationDate=x.FirstOrDefault()?.DonationDate};});}
    public async Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(string? s)=>(await _repo.GetRequestsAsync(status:string.IsNullOrWhiteSpace(s)?null:s.Trim())).Select(Mapping.Request);
    public async Task<BloodRequestResponseDto> CreateRequestAsync(Guid id,CreateBloodRequestDto d){var u=await _repo.FindUserAsync(id);var r=CreateRequest(id,"BloodBank",u?.FullName??"Blood Bank A",u?.Phone??u?.Email,d);await _repo.AddRequestAsync(r);await _repo.SaveChangesAsync();return Mapping.Request(r);}
    private static BloodRequest CreateRequest(Guid id,string type,string name,string? contact,CreateBloodRequestDto d)=>new(){Id=Guid.NewGuid(),RequesterId=id,RequesterType=type,RequesterName=name,RequesterContact=contact,HospitalName=d.HospitalName?.Trim(),PatientName=d.PatientName.Trim(),BloodGroup=d.BloodGroup.Trim().ToUpperInvariant(),ComponentType=d.ComponentType.Trim(),QuantityUnits=d.QuantityUnits,Urgency=d.Urgency,Status="Pending",HospitalDepartment=d.HospitalDepartment,MedicalReason=d.MedicalReason,CreatedAt=DateTime.UtcNow};
    public async Task<(BloodRequestResponseDto?,string?)> ProcessRequestAsync(Guid id,ProcessRequestDto d){var r=await _repo.FindRequestAsync(id);if(r==null)return(null,"Request not found.");if(d.Status=="Fulfilled"&&r.Status!="Fulfilled"){var s=await _repo.FindStockAsync(r.BloodGroup,r.ComponentType);if(s==null||s.QuantityUnits<r.QuantityUnits)return(null,$"Insufficient stock of {r.BloodGroup} ({r.ComponentType}). Available: {s?.QuantityUnits??0} units, Requested: {r.QuantityUnits} units.");var bags=await _repo.GetAvailableBagsAsync(r.BloodGroup,r.ComponentType,r.QuantityUnits);if(bags.Count<r.QuantityUnits)return(null,$"Insufficient individual bags for {r.BloodGroup} ({r.ComponentType}). Available bags: {bags.Count}, Requested: {r.QuantityUnits}.");foreach(var b in bags){b.IsIssued=true;b.IssuedDate=DateTime.UtcNow;}r.IssuedBagNumbers=string.Join(", ",bags.Select(b=>b.BagNumber));s.QuantityUnits-=r.QuantityUnits;s.LastUpdated=DateTime.UtcNow;}r.Status=d.Status;r.AdminComments=d.AdminComments;r.ProcessedAt=DateTime.UtcNow;await _repo.SaveChangesAsync();return(Mapping.Request(r),null);}
}
