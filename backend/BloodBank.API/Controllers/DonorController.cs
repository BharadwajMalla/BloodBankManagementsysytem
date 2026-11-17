using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Services;

namespace BloodBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Donor")]
public class DonorController(IDonorService service) : ControllerBase
{
    private Guid UserId()
    {
        return Guid.Parse((User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"))!.Value);
    }

    [HttpGet("camps")]
    public async Task<IActionResult> Camps()
    {
        var camps = await service.GetCampsAsync();
        return Ok(camps);
    }

    [HttpPost("request")]
    public async Task<IActionResult> SubmitRequest(CreateBloodRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var request = await service.CreateRequestAsync(UserId(), dto);
        return Ok(request);
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> Requests()
    {
        var requests = await service.GetRequestsAsync(UserId());
        return Ok(requests);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var profile = await service.GetProfileAsync(UserId());
        return profile == null
            ? NotFound(new { message = "Donor user profile not found." })
            : Ok(profile);
    }
}
