using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Services;
using BloodBank.API.Models;
namespace BloodBank.API.Controllers;
[ApiController,Route("api/[controller]"),Authorize(Roles="BloodBank,Admin")]
public class BloodBankController(IBloodBankService service):ControllerBase
{
    private Guid UserId()=>Guid.Parse((User.FindFirst(ClaimTypes.NameIdentifier)??User.FindFirst("sub"))!.Value);
    [HttpGet("stock")] public async Task<IActionResult> GetStock(string? bloodGroup,string? componentType)=>Ok(await service.GetStockAsync(bloodGroup,componentType));
    [HttpPost("stock")] public async Task<IActionResult> AddStock(AddStockDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.AddStockAsync(dto);return x==null?Conflict(new{message="A blood bag with this bag number already exists."}):Ok(x);}
    [HttpPost("replenish")] public async Task<IActionResult> ReplenishStock(ReplenishStockDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.ReplenishStockAsync(dto);return Ok(x);}
    [HttpPost("clear-all-data")] public async Task<IActionResult> ClearAllData(){await service.ClearAllDataAsync();return Ok(new{message="All operational data (stocks, bags, camps, requests) cleared successfully."});}
    [HttpGet("stock/bags")] public async Task<IActionResult> GetBags(string? componentType)=>Ok(await service.GetBagsAsync(componentType));
    [HttpGet("getcamps")] public async Task<IActionResult> GetCamps()=>Ok(await service.GetCampsAsync());
    [HttpGet("getcamps/{id:guid}")] public async Task<IActionResult> GetCamp(Guid id){var x=await service.GetCampAsync(id);return x==null?NotFound(new{message="Camp not found."}):Ok(x);}
    [HttpPost("addcamps")] public async Task<IActionResult> CreateCamp(CreateCampDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.CreateCampAsync(dto);return CreatedAtAction(nameof(GetCamp),new{id=x.Id},x);}
    [HttpPut("updatecamps/{id:guid}")] public async Task<IActionResult> UpdateCamp(Guid id,CreateCampDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.UpdateCampAsync(id,dto);return x==null?NotFound(new{message="Camp not found."}):Ok(x);}
    [HttpDelete("deletecamps/{id:guid}")] public async Task<IActionResult> DeleteCamp(Guid id)=>await service.DeleteCampAsync(id)?NoContent():NotFound(new{message="Camp not found."});
    [HttpGet("donors")] public async Task<IActionResult> GetDonors()=>Ok(await service.GetDonorsAsync());
    [HttpGet("requests")] public async Task<IActionResult> GetRequests(string? status)=>Ok(await service.GetRequestsAsync(status));
    [HttpPost("requests")] public async Task<IActionResult> CreateRequest(CreateBloodRequestDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var result=await service.CreateRequestAsync(UserId(),dto);return CreatedAtAction(nameof(GetRequests),new{id=result.Id},result);}
    [HttpPut("requests/{id}/process")] public async Task<IActionResult> Process(Guid id,ProcessRequestDto dto){var (result,error)=await service.ProcessRequestAsync(id,dto);return result==null?(error=="Request not found."?NotFound(new{message=error}):BadRequest(new{message=error})):Ok(result);}
}
