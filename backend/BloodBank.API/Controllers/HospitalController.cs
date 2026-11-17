using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Services;

namespace BloodBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Hospital")]
public class HospitalController(IHospitalService service) : ControllerBase
{
    private Guid UserId()
    {
        return Guid.Parse((User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"))!.Value);
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

    [HttpGet("available-stock-summary")]
    public async Task<IActionResult> Stock()
    {
        var stock = await service.GetStockAsync();
        return Ok(stock);
    }
}
