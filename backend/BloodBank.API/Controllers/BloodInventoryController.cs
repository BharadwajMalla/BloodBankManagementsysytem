using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Services;
namespace BloodBank.API.Controllers;
[ApiController,Route("api/[controller]")]
public class BloodInventoryController(IBloodInventoryService service):ControllerBase
{
    [HttpGet,AllowAnonymous] public async Task<IActionResult> GetAll(string? bloodGroup=null)=>Ok(await service.GetAllAsync(bloodGroup));
    [HttpPost,Authorize(Roles="Admin,Hospital")] public async Task<IActionResult> AddOrUpdate(BloodInventoryCreateDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);var x=await service.AddOrUpdateAsync(dto);return CreatedAtAction(nameof(GetAll),new{bloodGroup=x.BloodGroup},x);}
}
