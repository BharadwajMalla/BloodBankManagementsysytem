using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Services;
namespace BloodBank.API.Controllers;
[ApiController,Route("api/[controller]"),Authorize(Roles="BloodBank,Admin,TechnicalSupervisor,Technician")]
public class BloodBankController(IBloodBankService service):ControllerBase
{
    private Guid UserId()=>Guid.Parse((User.FindFirst(ClaimTypes.NameIdentifier)??User.FindFirst("sub"))!.Value);
    [HttpGet("stock")] public async Task<IActionResult> GetStock(string? bloodGroup,string? componentType)=>Ok(await service.GetStockAsync(bloodGroup,componentType));
    [HttpPost("stock")] public async Task<IActionResult> AddStock(AddStockDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.AddStockAsync(dto);return x==null?Conflict(new{message="A blood bag with this bag number already exists."}):Ok(x);}
    [HttpPost("replenish")] public async Task<IActionResult> ReplenishStock(ReplenishStockDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.ReplenishStockAsync(dto);return Ok(x);}
    [HttpPost("seed-live-data")] public async Task<IActionResult> SeedLiveData() {
          using var scope = HttpContext.RequestServices.CreateScope();
          var context = scope.ServiceProvider.GetRequiredService<BloodBank.API.Data.ApplicationDbContext>();
          await BloodBank.API.Data.DbInitializer.SeedLiveDataAsync(context);
          return Ok(new { message = "Live example data populated successfully." });
      }
      [HttpPost("clear-all-data")] public async Task<IActionResult> ClearAllData(){await service.ClearAllDataAsync();return Ok(new{message="All operational data (stocks, bags, camps, requests) cleared successfully."});}
    [HttpGet("stock/bags")] public async Task<IActionResult> GetBags(string? componentType)=>Ok(await service.GetBagsAsync(componentType));
    [HttpGet("getcamps")] public async Task<IActionResult> GetCamps()=>Ok(await service.GetCampsAsync());
    [HttpGet("getcamps/{id:guid}")] public async Task<IActionResult> GetCamp(Guid id){var x=await service.GetCampAsync(id);return x==null?NotFound(new{message="Camp not found."}):Ok(x);}
    [HttpPost("addcamps")] public async Task<IActionResult> CreateCamp(CreateCampDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.CreateCampAsync(dto);return CreatedAtAction(nameof(GetCamp),new{id=x.Id},x);}
    [HttpPut("updatecamps/{id:guid}")] public async Task<IActionResult> UpdateCamp(Guid id,CreateCampDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.UpdateCampAsync(id,dto);return x==null?NotFound(new{message="Camp not found."}):Ok(x);}
    [HttpDelete("deletecamps/{id:guid}")] public async Task<IActionResult> DeleteCamp(Guid id)=>await service.DeleteCampAsync(id)?NoContent():NotFound(new{message="Camp not found."});
    [HttpGet("donors")] public async Task<IActionResult> GetDonors()=>Ok(await service.GetDonorsAsync());
    [HttpPost("donors")] public async Task<IActionResult> CreateDonor(CreateDonorDto dto)
    {
        if(!ModelState.IsValid)return BadRequest(ModelState);
        var donor=await service.CreateDonorAsync(dto);
        return donor==null
            ? Conflict(new{message="A user with this email already exists."})
        : CreatedAtAction(nameof(GetDonors),new{id=donor.Donor.Id},donor);
    }
    [HttpPost("donors/{id:guid}/donation")] public async Task<IActionResult> CompleteDonation(Guid id, CompleteDonationDto dto)
    {
        var result = await service.CompleteDonationAsync(id, dto);
        return result.IsEligible ? Ok(result) : BadRequest(result);
    }
    [HttpGet("donors/{id:guid}/donations")] public async Task<IActionResult> GetDonationHistory(Guid id)
    {
        return Ok(await service.GetDonationHistoryAsync(id));
    }
    [HttpGet("requests")] public async Task<IActionResult> GetRequests(string? status)=>Ok(await service.GetRequestsAsync(status));
    [HttpPost("requests")] public async Task<IActionResult> CreateRequest(CreateBloodRequestDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var result=await service.CreateRequestAsync(UserId(),dto);return CreatedAtAction(nameof(GetRequests),new{id=result.Id},result);}
    [HttpPut("requests/{id}/process")] public async Task<IActionResult> Process(Guid id,ProcessRequestDto dto){var (result,error)=await service.ProcessRequestAsync(id,dto);return result==null?(error=="Request not found."?NotFound(new{message=error}):BadRequest(new{message=error})):Ok(result);}
    [HttpGet("components/collections")] public async Task<IActionResult> GetComponentCollections()=>Ok(await service.GetComponentCollectionsAsync());
    [HttpPost("components/collections")] public async Task<IActionResult> CreateComponentCollection(CreateComponentCollectionDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var (result,error)=await service.CreateComponentCollectionAsync(dto);return result==null?Conflict(new{message=error}):Ok(result);}
    [HttpPut("components/collections/{id:guid}")] public async Task<IActionResult> UpdateComponentCollection(Guid id,CreateComponentCollectionDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var (result,error)=await service.UpdateComponentCollectionAsync(id,dto);return result==null?BadRequest(new{message=error}):Ok(result);}
    [HttpDelete("components/collections/{id:guid}")] public async Task<IActionResult> DeleteComponentCollection(Guid id)=>await service.DeleteComponentCollectionAsync(id)?NoContent():NotFound(new{message="Collection not found or cannot be deleted."});
    [HttpGet("components/discards")] public async Task<IActionResult> GetComponentDiscards()=>Ok(await service.GetComponentDiscardsAsync());
    [HttpPost("components/discards")] public async Task<IActionResult> CreateComponentDiscard(CreateComponentDiscardDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var (result,error)=await service.CreateComponentDiscardAsync(dto);return result==null?BadRequest(new{message=error}):Ok(result);}
    [HttpPut("components/discards/{id:guid}/review"),Authorize(Roles="Admin")] public async Task<IActionResult> ReviewComponentDiscard(Guid id,[FromQuery] bool approve){var (result,error)=await service.ReviewComponentDiscardAsync(id,approve,User.Identity?.Name??"Admin");return result==null?BadRequest(new{message=error}):Ok(result);}
    [HttpDelete("components/discards/{id:guid}")] public async Task<IActionResult> DeleteComponentDiscard(Guid id)=>await service.DeleteComponentDiscardAsync(id)?NoContent():NotFound();
    [HttpGet("components/returns")] public async Task<IActionResult> GetComponentReturns()=>Ok(await service.GetComponentReturnsAsync());
    [HttpPost("components/returns")] public async Task<IActionResult> CreateComponentReturn(CreateComponentReturnDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var (result,error)=await service.CreateComponentReturnAsync(dto);return result==null?BadRequest(new{message=error}):Ok(result);}
    [HttpDelete("components/returns/{id:guid}")] public async Task<IActionResult> DeleteComponentReturn(Guid id)=>await service.DeleteComponentReturnAsync(id)?NoContent():NotFound();

    [HttpPut("stock/{id:guid}")] public async Task<IActionResult> UpdateStock(Guid id, UpdateStockDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var s=await service.UpdateStockAsync(id,dto);return s==null?NotFound(new{message="Stock record not found."}):Ok(s);}
    [HttpPut("donors/{id:guid}")] public async Task<IActionResult> UpdateDonor(Guid id, UpdateDonorDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var d=await service.UpdateDonorAsync(id,dto);return d==null?BadRequest(new{message="Donor not found or email already registered."}):Ok(d);}
    [HttpPut("requests/{id:guid}")] public async Task<IActionResult> UpdateRequest(Guid id, UpdateBloodRequestDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var r=await service.UpdateRequestAsync(id,dto);return r==null?NotFound(new{message="Request not found."}):Ok(r);}
    [HttpPut("components/discards/{id:guid}")] public async Task<IActionResult> UpdateComponentDiscard(Guid id, UpdateComponentDiscardDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var (r,err)=await service.UpdateComponentDiscardAsync(id,dto);return r==null?BadRequest(new{message=err}):Ok(r);}
    [HttpPut("components/returns/{id:guid}")] public async Task<IActionResult> UpdateComponentReturn(Guid id, UpdateComponentReturnDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var (r,err)=await service.UpdateComponentReturnAsync(id,dto);return r==null?BadRequest(new{message=err}):Ok(r);}

    // ==========================================
    // Uddanam Blood Centre Register Endpoints
    // ==========================================
    [HttpGet("registers/tti")] public async Task<IActionResult> GetTtiScreenings(string? status) => Ok(await service.GetTtiScreeningsAsync(status));
    [HttpPost("registers/tti")] public async Task<IActionResult> CreateTtiScreening(CreateTtiScreeningDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.CreateTtiScreeningAsync(dto); return Ok(res); }
    [HttpPut("registers/tti/{id:guid}")] public async Task<IActionResult> UpdateTtiScreening(Guid id, UpdateTtiScreeningDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.UpdateTtiScreeningAsync(id, dto); return res == null ? NotFound(new{message="Record not found."}) : Ok(res); }
    [HttpDelete("registers/tti/{id:guid}")] public async Task<IActionResult> DeleteTtiScreening(Guid id) => await service.DeleteTtiScreeningAsync(id) ? NoContent() : NotFound();

    [HttpGet("registers/grouping")] public async Task<IActionResult> GetPatientGroupings() => Ok(await service.GetPatientGroupingsAsync());
    [HttpPost("registers/grouping")] public async Task<IActionResult> CreatePatientGrouping(CreatePatientGroupingDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.CreatePatientGroupingAsync(dto); return Ok(res); }
    [HttpPut("registers/grouping/{id:guid}")] public async Task<IActionResult> UpdatePatientGrouping(Guid id, UpdatePatientGroupingDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.UpdatePatientGroupingAsync(id, dto); return res == null ? NotFound(new{message="Record not found."}) : Ok(res); }
    [HttpDelete("registers/grouping/{id:guid}")] public async Task<IActionResult> DeletePatientGrouping(Guid id) => await service.DeletePatientGroupingAsync(id) ? NoContent() : NotFound();

    [HttpGet("registers/bag-stock")] public async Task<IActionResult> GetBloodBagStockRegisters() => Ok(await service.GetBloodBagStockRegistersAsync());
    [HttpPost("registers/bag-stock")] public async Task<IActionResult> CreateBloodBagStockRegister(CreateBloodBagStockRegisterDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.CreateBloodBagStockRegisterAsync(dto); return Ok(res); }
    [HttpPut("registers/bag-stock/{id:guid}")] public async Task<IActionResult> UpdateBloodBagStockRegister(Guid id, UpdateBloodBagStockRegisterDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.UpdateBloodBagStockRegisterAsync(id, dto); return res == null ? NotFound(new{message="Record not found."}) : Ok(res); }
    [HttpDelete("registers/bag-stock/{id:guid}")] public async Task<IActionResult> DeleteBloodBagStockRegister(Guid id) => await service.DeleteBloodBagStockRegisterAsync(id) ? NoContent() : NotFound();

    [HttpGet("registers/diagnostic-kits")] public async Task<IActionResult> GetDiagnosticKitRegisters() => Ok(await service.GetDiagnosticKitRegistersAsync());
    [HttpPost("registers/diagnostic-kits")] public async Task<IActionResult> CreateDiagnosticKitRegister(CreateDiagnosticKitRegisterDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.CreateDiagnosticKitRegisterAsync(dto); return Ok(res); }
    [HttpPut("registers/diagnostic-kits/{id:guid}")] public async Task<IActionResult> UpdateDiagnosticKitRegister(Guid id, UpdateDiagnosticKitRegisterDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.UpdateDiagnosticKitRegisterAsync(id, dto); return res == null ? NotFound(new{message="Record not found."}) : Ok(res); }
    [HttpDelete("registers/diagnostic-kits/{id:guid}")] public async Task<IActionResult> DeleteDiagnosticKitRegister(Guid id) => await service.DeleteDiagnosticKitRegisterAsync(id) ? NoContent() : NotFound();

    [HttpGet("registers/apheresis")] public async Task<IActionResult> GetApheresisRecords() => Ok(await service.GetApheresisRecordsAsync());
    [HttpPost("registers/apheresis")] public async Task<IActionResult> CreateApheresisRecord(CreateApheresisRecordDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.CreateApheresisRecordAsync(dto); return Ok(res); }
    [HttpPut("registers/apheresis/{id:guid}")] public async Task<IActionResult> UpdateApheresisRecord(Guid id, UpdateApheresisRecordDto dto) { if(!ModelState.IsValid) return BadRequest(ModelState); var res = await service.UpdateApheresisRecordAsync(id, dto); return res == null ? NotFound(new{message="Record not found."}) : Ok(res); }
    [HttpDelete("registers/apheresis/{id:guid}")] public async Task<IActionResult> DeleteApheresisRecord(Guid id) => await service.DeleteApheresisRecordAsync(id) ? NoContent() : NotFound();

    [HttpGet("registers/master-record")] public async Task<IActionResult> GetMasterRecords() => Ok(await service.GetMasterRecordsAsync());
}
