using System.Security.Claims;
using BloodBank.API.DTOs;
using BloodBank.Application.Contracts;
using BloodBank.Application.Dtos;
using BloodBank.Application.Models;
using BloodBank.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.Services;

namespace BloodBank.API.Controllers;

[ApiController]
[Route("api/master-data")]
[Authorize(Roles = "Admin,BloodBank")]
public sealed class MasterDataController(
    IMasterDataService service,
    ISqlExceptionLogger exceptionLogger) : ControllerBase
{
    [HttpGet("{type}")]
    public async Task<IActionResult> Get(
        string type,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(type, out var category))
            return InvalidType(type);

        var records = await service.GetAsync(category, search, cancellationToken);
        return Ok(records);
    }

    [HttpPost("{type}")]
    public async Task<IActionResult> Create(
        string type,
        MasterDataCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(type, out var category))
            return InvalidType(type);
        if (category == MasterDataCategory.Staff && !User.IsInRole("Admin"))
            return Forbid();

        try
        {
            var record = await service.CreateAsync(category, command, Actor, cancellationToken);
            return CreatedAtAction(nameof(Get), new { type, search = (string?)null }, record);
        }
        catch (ArgumentException exception)
        {
            await exceptionLogger.LogAsync(exception, nameof(Create));
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid master-data record", detail: exception.Message);
        }
        catch (MasterDataConflictException exception)
        {
            await exceptionLogger.LogAsync(exception, nameof(Create));
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Master-data conflict", detail: exception.Message);
        }
    }

    [HttpPut("{type}/{id:guid}")]
    public async Task<IActionResult> Update(
        string type,
        Guid id,
        MasterDataCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(type, out var category))
            return InvalidType(type);
        if (category == MasterDataCategory.Staff && !User.IsInRole("Admin"))
            return Forbid();

        try
        {
            var record = await service.UpdateAsync(category, id, command, Actor, cancellationToken);
            return Ok(record);
        }
        catch (ArgumentException exception)
        {
            await exceptionLogger.LogAsync(exception, nameof(Update));
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid master-data record", detail: exception.Message);
        }
        catch (MasterDataConflictException exception)
        {
            await exceptionLogger.LogAsync(exception, nameof(Update));
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Master-data conflict", detail: exception.Message);
        }
        catch (MasterDataNotFoundException exception)
        {
            await exceptionLogger.LogAsync(exception, nameof(Update));
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Master-data record not found", detail: exception.Message);
        }
    }

    [HttpPatch("{type}/{id:guid}/active")]
    public async Task<IActionResult> SetActive(
        string type,
        Guid id,
        MasterDataActiveDto request,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(type, out var category))
            return InvalidType(type);
        if (category == MasterDataCategory.Staff && !User.IsInRole("Admin"))
            return Forbid();

        try
        {
            await service.SetActiveAsync(category, id, request.IsActive, Actor, cancellationToken);
            return NoContent();
        }
        catch (MasterDataNotFoundException exception)
        {
            await exceptionLogger.LogAsync(exception, nameof(SetActive));
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Master-data record not found", detail: exception.Message);
        }
    }

    private string Actor =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue("email")
        ?? User.Identity?.Name
        ?? "System";

    private IActionResult InvalidType(string type) =>
        Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Unknown master-data category",
            detail: $"'{type}' is not a supported master-data category.");

    private static bool TryParseCategory(string type, out MasterDataCategory category)
    {
        category = type.Trim().ToLowerInvariant() switch
        {
            "hospitals" => MasterDataCategory.Hospital,
            "doctors" => MasterDataCategory.Doctor,
            "staff" => MasterDataCategory.Staff,
            "products" => MasterDataCategory.Product,
            "bag-types" => MasterDataCategory.BagType,
            "suppliers" => MasterDataCategory.Supplier,
            _ => (MasterDataCategory)(-1)
        };
        return Enum.IsDefined(category);
    }
}
