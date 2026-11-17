using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Repositories;

namespace BloodBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,BloodBank")]
public class AuditReportController(ILoginAuditRepository loginAuditRepository) : ControllerBase
{
    [HttpGet("logins")]
    [ProducesResponseType(typeof(IReadOnlyList<LoginAuditResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<LoginAuditResponse>>> GetLogins(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery, Range(1, 500)] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest(new { message = "The 'from' date must be earlier than or equal to the 'to' date." });
        }

        var records = await loginAuditRepository.GetAsync(from, to, limit, cancellationToken);
        var response = records
            .Select(record => new LoginAuditResponse(
                record.Id,
                record.UserId,
                record.Email,
                record.FullName,
                record.Role,
                new DateTimeOffset(DateTime.SpecifyKind(record.LoginAt, DateTimeKind.Utc)),
                record.Succeeded,
                record.IpAddress,
                record.UserAgent))
            .ToList();

        return Ok(response);
    }
}