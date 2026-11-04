using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Services;
namespace BloodBank.API.Controllers;
[ApiController,Route("api/[controller]"),Authorize(Roles="Donor")]
public class DonorController(IDonorService service):ControllerBase
{
    private Guid UserId()=>Guid.Parse((User.FindFirst(ClaimTypes.NameIdentifier)??User.FindFirst("sub"))!.Value);
    [HttpGet("camps")] public async Task<IActionResult> Camps()=>Ok(await service.GetCampsAsync());
    [HttpPost("request")] public async Task<IActionResult> SubmitRequest(CreateBloodRequestDto dto){if(!ModelState.IsValid)return BadRequest(ModelState);return Ok(await service.CreateRequestAsync(UserId(),dto));}
    [HttpGet("my-requests")] public async Task<IActionResult> Requests()=>Ok(await service.GetRequestsAsync(UserId()));
    [HttpGet("profile")] public async Task<IActionResult> Profile(){var x=await service.GetProfileAsync(UserId());return x==null?NotFound(new{message="Donor user profile not found."}):Ok(x);}
}
