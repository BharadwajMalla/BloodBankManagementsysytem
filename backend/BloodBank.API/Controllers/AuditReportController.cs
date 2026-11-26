using System.Data.Common;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Repositories;
using BloodBank.API.Services;

namespace BloodBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,BloodBank")]
public class AuditReportController(
    ILoginAuditRepository loginAuditRepository,
    ILogger<AuditReportController> logger,
    ISqlExceptionLogger exceptionLogger) : ControllerBase
{
    [HttpGet("logins")]
    [ProducesResponseType(typeof(IReadOnlyList<LoginAuditResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        try
        {
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
        catch (Exception ex) when (ex is DbException or TimeoutException)
        {
            await exceptionLogger.LogAsync(ex, nameof(GetLogins));
            logger.LogError(ex, "Failed to retrieve login audit report");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to retrieve audit report");
        }
    }
}