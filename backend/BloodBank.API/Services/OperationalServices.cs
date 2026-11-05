using BloodBank.API.DTOs;
using BloodBank.API.Models;
using BloodBank.API.Repositories;

namespace BloodBank.API.Services;
public sealed class HospitalService : IHospitalService
{
    private readonly IBloodBankRepository _r;
    public HospitalService(IBloodBankRepository r)=>_r=r;
    public async Task<BloodRequestResponseDto> CreateRequestAsync(Guid id,CreateBloodRequestDto d){var u=await _r.FindUserAsync(id);var r=new BloodRequest{Id=Guid.NewGuid(),RequesterId=id,RequesterType="Hospital",RequesterName=u?.HospitalName??u?.FullName??"Partner Hospital",RequesterContact=u?.Phone??u?.Email,HospitalName=d.HospitalName?.Trim()??u?.HospitalName??u?.FullName,PatientName=d.PatientName.Trim(),BloodGroup=d.BloodGroup.Trim().ToUpperInvariant(),ComponentType=d.ComponentType.Trim(),QuantityUnits=d.QuantityUnits,Urgency=d.Urgency,Status="Pending",HospitalDepartment=d.HospitalDepartment?.Trim(),MedicalReason=d.MedicalReason?.Trim(),CreatedAt=DateTime.UtcNow};await _r.AddRequestAsync(r);await _r.SaveChangesAsync();return Mapping.Request(r);}
    public async Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(Guid id)=>(await _r.GetRequestsAsync(id)).Select(Mapping.Request);
    public async Task<IEnumerable<BloodStockDto>> GetStockAsync()=>(await _r.GetStocksAsync(null,null)).Where(s=>s.QuantityUnits>0).Select(s=>new BloodStockDto{Id=s.Id,BloodGroup=s.BloodGroup,ComponentType=s.ComponentType,QuantityUnits=s.QuantityUnits,BloodBankName=s.BloodBankName,LastUpdated=s.LastUpdated});
}
public sealed class DonorService : IDonorService
{
    private readonly IBloodBankRepository _r;
    public DonorService(IBloodBankRepository r)=>_r=r;
    public Task<IEnumerable<DonationCamp>> GetCampsAsync()=>_r.GetCampsAsync(true).ContinueWith(t=>(IEnumerable<DonationCamp>)t.Result);
    public async Task<BloodRequestResponseDto> CreateRequestAsync(Guid id,CreateBloodRequestDto d){var u=await _r.FindUserAsync(id);var r=new BloodRequest{Id=Guid.NewGuid(),RequesterId=id,RequesterType="Donor",RequesterName=u?.FullName??"Registered Donor",RequesterContact=u?.Phone??u?.Email,PatientName=d.PatientName.Trim(),BloodGroup=d.BloodGroup.Trim().ToUpperInvariant(),ComponentType=d.ComponentType.Trim(),QuantityUnits=d.QuantityUnits,Urgency=d.Urgency,Status="Pending",HospitalDepartment=d.HospitalDepartment,MedicalReason=d.MedicalReason,CreatedAt=DateTime.UtcNow};await _r.AddRequestAsync(r);await _r.SaveChangesAsync();return Mapping.Request(r);}
    public async Task<IEnumerable<BloodRequestResponseDto>> GetRequestsAsync(Guid id)=>(await _r.GetRequestsAsync(id)).Select(Mapping.Request);
    public async Task<DonorDetailsDto?> GetProfileAsync(Guid id){var u=await _r.FindUserAsync(id);if(u==null)return null;var rs=await _r.GetDonationsAsync(new[]{id});var last=rs.OrderByDescending(x=>x.DonationDate).FirstOrDefault();return new DonorDetailsDto{Id=u.Id,FullName=u.FullName,Email=u.Email,Phone=u.Phone,DateOfBirth=u.DateOfBirth,Sex=u.Sex,Address=u.Address,BloodGroup=u.BloodGroup,RegisteredDate=u.CreatedAt,TotalDonations=rs.Count,LastDonationDate=last?.DonationDate};}
}
public sealed class BloodInventoryService : IBloodInventoryService
{
    private readonly IBloodBankRepository _r;
    public BloodInventoryService(IBloodBankRepository r)=>_r=r;
    public async Task<IEnumerable<BloodInventoryResponseDto>> GetAllAsync(string? g)=>(await _r.GetInventoryAsync(string.IsNullOrWhiteSpace(g)?null:g.Trim().ToUpperInvariant())).Select(x=>new BloodInventoryResponseDto{Id=x.Id,BloodGroup=x.BloodGroup,QuantityUnits=x.QuantityUnits,BloodBankName=x.BloodBankName,LastUpdated=x.LastUpdated});
    public async Task<BloodInventoryResponseDto> AddOrUpdateAsync(BloodInventoryCreateDto d){var g=d.BloodGroup.Trim().ToUpperInvariant();var bank=d.BloodBankName.Trim();var x=await _r.FindInventoryAsync(g,bank);if(x==null){x=new BloodInventory{Id=Guid.NewGuid(),BloodGroup=g,QuantityUnits=d.QuantityUnits,BloodBankName=bank,LastUpdated=DateTime.UtcNow};await _r.AddInventoryAsync(x);}else{x.QuantityUnits+=d.QuantityUnits;x.LastUpdated=DateTime.UtcNow;}await _r.SaveChangesAsync();return new BloodInventoryResponseDto{Id=x.Id,BloodGroup=x.BloodGroup,QuantityUnits=x.QuantityUnits,BloodBankName=x.BloodBankName,LastUpdated=x.LastUpdated};}
}
