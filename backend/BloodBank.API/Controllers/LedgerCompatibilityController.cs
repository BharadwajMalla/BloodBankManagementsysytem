using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.Services;
using BloodBank.Application.Contracts;

namespace BloodBank.API.Controllers;

[ApiController]
[Authorize(Roles = "BloodBank,Admin,TechnicalSupervisor,Technician")]
public class LedgerCompatibilityController(
    IBloodBankService service,
    IDonorDonationService donorDonationService) : ControllerBase
{
    [HttpGet("api/donors")]
    public async Task<IActionResult> GetDonors(CancellationToken cancellationToken)
    {
        return Ok(await donorDonationService.GetDonorsAsync(cancellationToken));
    }

    [HttpGet("api/components")]
    public async Task<IActionResult> GetComponents()
    {
        return Ok(await service.GetComponentCollectionsAsync());
    }

    [HttpGet("api/components/stock")]
    public async Task<IActionResult> GetStock()
    {
        return Ok(await service.GetStockAsync(null, null));
    }

    [HttpGet("api/issues")]
    public async Task<IActionResult> GetIssues([FromQuery] string? status)
    {
        return Ok(await service.GetRequestsAsync(status));
    }
}
