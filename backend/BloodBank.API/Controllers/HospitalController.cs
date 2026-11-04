using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Services;
namespace BloodBank.API.Controllers;
[ApiController,Route("api/[controller]"),Authorize(Roles="Hospital")]
public class HospitalController(IHospitalService service):ControllerBase
{
    private Guid UserId()=>Guid.Parse((User.FindFirst(ClaimTypes.NameIdentifier)??User.FindFirst("sub"))!.Value);
    [HttpPost("request")] public async Task<IActionResult> SubmitRequest(CreateBloodRequestDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);return Ok(await service.CreateRequestAsync(UserId(),dto));}
    [HttpGet("my-requests")] public async Task<IActionResult> Requests()=>Ok(await service.GetRequestsAsync(UserId()));
    [HttpGet("available-stock-summary")] public async Task<IActionResult> Stock()=>Ok(await service.GetStockAsync());
}
