using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Mapping;
using BloodBank.API.Repositories;
using BloodBank.API.Services;
using BloodBank.Application.Contracts;

namespace BloodBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "BloodBank,Admin,TechnicalSupervisor,Technician")]
public class BloodBankController(
    IBloodBankService service,
    IDonorDonationService donorDonationService,
    ISqlExceptionLogger exceptionLogger) : ControllerBase
{
    private Guid UserId()
    {
        return Guid.Parse((User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"))!.Value);
    }

    [HttpGet("getstock")]
    public async Task<IActionResult> GetStock(string? bloodGroup, string? componentType)
    {
        var stock = await service.GetStockAsync(bloodGroup, componentType);
        return Ok(stock);
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStockAlias(string? bloodGroup, string? componentType)
    {
        var stock = await service.GetStockAsync(bloodGroup, componentType);
        return Ok(stock);
    }

    [HttpPost("addstock")]
    public async Task<IActionResult> AddStock(AddStockDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var stock = await service.AddStockAsync(dto);
        return stock == null
            ? Conflict(new { message = "A blood bag with this bag number already exists." })
            : Ok(stock);
    }

    [HttpPost("stock")]
    public async Task<IActionResult> AddStockAlias(AddStockDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var stock = await service.AddStockAsync(dto);
        return stock == null
            ? Conflict(new { message = "A blood bag with this bag number already exists." })
            : Ok(stock);
    }

    [HttpPost("replenish")]
    public async Task<IActionResult> ReplenishStock(ReplenishStockDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var stock = await service.ReplenishStockAsync(dto);
        return Ok(stock);
    }

    [HttpPost("seed-live-data")]
    public async Task<IActionResult> SeedLiveData()
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBloodBankRepository>();
        await BloodBank.API.Data.DbInitializer.SeedLiveDataAsync(repository);
        return Ok(new { message = "Live example data populated successfully." });
    }

    [HttpPost("clear-all-data")]
    public async Task<IActionResult> ClearAllData()
    {
        await service.ClearAllDataAsync();
        return Ok(new { message = "All operational data (stocks, bags, camps, requests) cleared successfully." });
    }

    [HttpGet("stock/bags")]
    public async Task<IActionResult> GetBags(string? componentType)
    {
        var bags = await service.GetBagsAsync(componentType);
        return Ok(bags);
    }

    [HttpGet("getcamps")]
    public async Task<IActionResult> GetCamps()
    {
        var camps = await service.GetCampsAsync();
        return Ok(camps);
    }

    [HttpGet("getcamps/{id:guid}")]
    public async Task<IActionResult> GetCamp(Guid id)
    {
        var camp = await service.GetCampAsync(id);
        return camp == null
            ? NotFound(new { message = "Camp not found." })
            : Ok(camp);
    }

    [HttpPost("addcamps")]
    public async Task<IActionResult> CreateCamp(CreateCampDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var camp = await service.CreateCampAsync(dto);
        return CreatedAtAction(nameof(GetCamp), new { id = camp.Id }, camp);
    }

    [HttpPut("updatecamps/{id:guid}")]
    public async Task<IActionResult> UpdateCamp(Guid id, CreateCampDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var camp = await service.UpdateCampAsync(id, dto);
        return camp == null
            ? NotFound(new { message = "Camp not found." })
            : Ok(camp);
    }

    [HttpDelete("deletecamps/{id:guid}")]
    public async Task<IActionResult> DeleteCamp(Guid id)
    {
        var deleted = await service.DeleteCampAsync(id);
        return deleted
            ? NoContent()
            : NotFound(new { message = "Camp not found." });
    }

    [HttpGet("donors")]
    public async Task<IActionResult> GetDonors(CancellationToken cancellationToken)
    {
        var donors = await donorDonationService.GetDonorsAsync(cancellationToken);
        return Ok(donors);
    }

    [HttpDelete("donors/{id:guid}")]
    public async Task<IActionResult> DeactivateDonor(Guid id, CancellationToken cancellationToken)
    {
        var deactivated = await donorDonationService.DeactivateDonorAsync(
            id,
            cancellationToken,
            User.Identity?.Name);
        return deactivated
            ? NoContent()
            : NotFound(new { message = "Donor not found or already inactive." });
    }

    [HttpPost("donors")]
    public async Task<IActionResult> CreateDonor(CreateDonorDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var donor = await donorDonationService.CreateDonorAsync(
            DonorDonationApiMapper.ToCommand(dto),
            cancellationToken,
            User.Identity?.Name);
        return donor == null
            ? Conflict(new { message = "A user with this email already exists." })
            : CreatedAtAction(nameof(GetDonors), new { id = donor.Donor.Id }, donor);
    }

    [HttpPost("donors/{id:guid}/donation")]
    public async Task<IActionResult> CompleteDonation(
        Guid id,
        CompleteDonationDto dto,
        CancellationToken cancellationToken)
    {
        var result = await donorDonationService.CompleteDonationAsync(
            id,
            DonorDonationApiMapper.ToCommand(dto),
            cancellationToken);
        return result.IsEligible
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpGet("donors/{id:guid}/donations")]
    public async Task<IActionResult> GetDonationHistory(Guid id, CancellationToken cancellationToken)
    {
        var donations = await donorDonationService.GetDonationHistoryAsync(id, cancellationToken);
        return Ok(donations);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(string? status)
    {
        var requests = await service.GetRequestsAsync(status);
        return Ok(requests);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest(CreateBloodRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.CreateRequestAsync(UserId(), dto);
        return CreatedAtAction(nameof(GetRequests), new { id = result.Id }, result);
    }

    [HttpPut("requests/{id}/process")]
    public async Task<IActionResult> Process(Guid id, ProcessRequestDto dto)
    {
        var (result, error) = await service.ProcessRequestAsync(id, dto, User.Identity?.Name);
        if (result != null)
        {
            return Ok(result);
        }

        return error == "Request not found."
            ? NotFound(new { message = error })
            : BadRequest(new { message = error });
    }

    [HttpGet("components/collections")]
    public async Task<IActionResult> GetComponentCollections()
    {
        var collections = await service.GetComponentCollectionsAsync();
        return Ok(collections);
    }

    [HttpPost("components/collections")]
    public async Task<IActionResult> CreateComponentCollection(CreateComponentCollectionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, error) = await service.CreateComponentCollectionAsync(dto);
        return result == null
            ? Conflict(new { message = error })
            : Ok(result);
    }

    [HttpPut("components/collections/{id:guid}")]
    public async Task<IActionResult> UpdateComponentCollection(Guid id, CreateComponentCollectionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, error) = await service.UpdateComponentCollectionAsync(id, dto);
        return result == null
            ? BadRequest(new { message = error })
            : Ok(result);
    }

    [HttpDelete("components/collections/{id:guid}")]
    public async Task<IActionResult> DeleteComponentCollection(Guid id)
    {
        var deleted = await service.DeleteComponentCollectionAsync(id);
        return deleted
            ? NoContent()
            : NotFound(new { message = "Collection not found or cannot be deleted." });
    }

    [HttpGet("components/discards")]
    public async Task<IActionResult> GetComponentDiscards()
    {
        var discards = await service.GetComponentDiscardsAsync();
        return Ok(discards);
    }

    [HttpPost("components/discards")]
    public async Task<IActionResult> CreateComponentDiscard(CreateComponentDiscardDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, error) = await service.CreateComponentDiscardAsync(dto);
        return result == null
            ? BadRequest(new { message = error })
            : Ok(result);
    }

    [HttpPut("components/discards/{id:guid}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewComponentDiscard(Guid id, [FromQuery] bool approve)
    {
        var (result, error) = await service.ReviewComponentDiscardAsync(id, approve, User.Identity?.Name ?? "Admin");
        return result == null
            ? BadRequest(new { message = error })
            : Ok(result);
    }

    [HttpDelete("components/discards/{id:guid}")]
    public async Task<IActionResult> DeleteComponentDiscard(Guid id)
    {
        var deleted = await service.DeleteComponentDiscardAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("components/returns")]
    public async Task<IActionResult> GetComponentReturns()
    {
        var returns = await service.GetComponentReturnsAsync();
        return Ok(returns);
    }

    [HttpPost("components/returns")]
    public async Task<IActionResult> CreateComponentReturn(CreateComponentReturnDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, error) = await service.CreateComponentReturnAsync(dto);
        return result == null
            ? BadRequest(new { message = error })
            : Ok(result);
    }

    [HttpDelete("components/returns/{id:guid}")]
    public IActionResult DeleteComponentReturn(Guid id)
    {
        return Conflict(new
        {
            message = "Component return records cannot be deleted because deletion would desynchronize bag status and stock."
        });
    }

    [HttpPut("stock/{id:guid}")]
    public async Task<IActionResult> UpdateStock(Guid id, UpdateStockDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var stock = await service.UpdateStockAsync(id, dto);
        return stock == null
            ? NotFound(new { message = "Stock record not found." })
            : Ok(stock);
    }

    [HttpPut("donors/{id:guid}")]
    public async Task<IActionResult> UpdateDonor(
        Guid id,
        UpdateDonorDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var donor = await donorDonationService.UpdateDonorAsync(
            id,
            DonorDonationApiMapper.ToCommand(dto),
            cancellationToken,
            User.Identity?.Name);
        return donor == null
            ? BadRequest(new { message = "Donor not found or email already registered." })
            : Ok(donor);
    }

    [HttpPut("requests/{id:guid}")]
    public async Task<IActionResult> UpdateRequest(Guid id, UpdateBloodRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var request = await service.UpdateRequestAsync(id, dto);
        return request == null
            ? NotFound(new { message = "Request not found." })
            : Ok(request);
    }

    [HttpDelete("requests/{id:guid}")]
    public async Task<IActionResult> DeletePendingRequest(Guid id)
    {
        var deleted = await service.DeletePendingRequestAsync(id);
        if (deleted is null)
        {
            return NotFound(new { message = "Request not found." });
        }

        return deleted.Value
            ? NoContent()
            : Conflict(new { message = "Only pending, unprocessed requests can be deleted." });
    }

    [HttpPut("components/discards/{id:guid}")]
    public async Task<IActionResult> UpdateComponentDiscard(Guid id, UpdateComponentDiscardDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, error) = await service.UpdateComponentDiscardAsync(id, dto);
        return result == null
            ? BadRequest(new { message = error })
            : Ok(result);
    }

    [HttpPut("components/returns/{id:guid}")]
    public async Task<IActionResult> UpdateComponentReturn(Guid id, UpdateComponentReturnDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, error) = await service.UpdateComponentReturnAsync(id, dto);
        return result == null
            ? BadRequest(new { message = error })
            : Ok(result);
    }

    [HttpGet("registers/tti")]
    public async Task<IActionResult> GetTtiScreenings(string? status)
    {
        var records = await service.GetTtiScreeningsAsync(status);
        return Ok(records);
    }

    [HttpPost("registers/tti")]
    public async Task<IActionResult> CreateTtiScreening(CreateTtiScreeningDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.CreateTtiScreeningAsync(dto);
        return Ok(result);
    }

    [HttpPut("registers/tti/{id:guid}")]
    public async Task<IActionResult> UpdateTtiScreening(Guid id, UpdateTtiScreeningDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.UpdateTtiScreeningAsync(id, dto);
        return result == null
            ? NotFound(new { message = "Record not found." })
            : Ok(result);
    }

    [HttpDelete("registers/tti/{id:guid}")]
    public async Task<IActionResult> DeleteTtiScreening(Guid id)
    {
        var deleted = await service.DeleteTtiScreeningAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("registers/grouping")]
    public async Task<IActionResult> GetPatientGroupings()
    {
        var records = await service.GetPatientGroupingsAsync();
        return Ok(records);
    }

    [HttpPost("registers/grouping")]
    public async Task<IActionResult> CreatePatientGrouping(CreatePatientGroupingDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.CreatePatientGroupingAsync(dto);
        return Ok(result);
    }

    [HttpPut("registers/grouping/{id:guid}")]
    public async Task<IActionResult> UpdatePatientGrouping(Guid id, UpdatePatientGroupingDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.UpdatePatientGroupingAsync(id, dto);
        return result == null
            ? NotFound(new { message = "Record not found." })
            : Ok(result);
    }

    [HttpDelete("registers/grouping/{id:guid}")]
    public async Task<IActionResult> DeletePatientGrouping(Guid id)
    {
        var deleted = await service.DeletePatientGroupingAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("registers/bag-stock")]
    public async Task<IActionResult> GetBloodBagStockRegisters()
    {
        var records = await service.GetBloodBagStockRegistersAsync();
        return Ok(records);
    }

    [HttpPost("registers/bag-stock")]
    public async Task<IActionResult> CreateBloodBagStockRegister(CreateBloodBagStockRegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.CreateBloodBagStockRegisterAsync(dto);
        return Ok(result);
    }

    [HttpPut("registers/bag-stock/{id:guid}")]
    public async Task<IActionResult> UpdateBloodBagStockRegister(Guid id, UpdateBloodBagStockRegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.UpdateBloodBagStockRegisterAsync(id, dto);
        return result == null
            ? NotFound(new { message = "Record not found." })
            : Ok(result);
    }

    [HttpDelete("registers/bag-stock/{id:guid}")]
    public async Task<IActionResult> DeleteBloodBagStockRegister(Guid id)
    {
        var deleted = await service.DeleteBloodBagStockRegisterAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("registers/diagnostic-kits")]
    public async Task<IActionResult> GetDiagnosticKitRegisters()
    {
        var records = await service.GetDiagnosticKitRegistersAsync();
        return Ok(records);
    }

    [HttpPost("registers/diagnostic-kits")]
    public async Task<IActionResult> CreateDiagnosticKitRegister(CreateDiagnosticKitRegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.CreateDiagnosticKitRegisterAsync(dto);
        return Ok(result);
    }

    [HttpPut("registers/diagnostic-kits/{id:guid}")]
    public async Task<IActionResult> UpdateDiagnosticKitRegister(Guid id, UpdateDiagnosticKitRegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await service.UpdateDiagnosticKitRegisterAsync(id, dto);
        return result == null
            ? NotFound(new { message = "Record not found." })
            : Ok(result);
    }

    [HttpDelete("registers/diagnostic-kits/{id:guid}")]
    public async Task<IActionResult> DeleteDiagnosticKitRegister(Guid id)
    {
        var deleted = await service.DeleteDiagnosticKitRegisterAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("registers/apheresis")]
    public async Task<IActionResult> GetApheresisRecords()
    {
        var records = await service.GetApheresisRecordsAsync();
        return Ok(records);
    }

    [HttpPost("registers/apheresis")]
    public async Task<IActionResult> CreateApheresisRecord(CreateApheresisRecordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            var result = await service.CreateApheresisRecordAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            await exceptionLogger.LogAsync(ex, nameof(CreateApheresisRecord));
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("registers/apheresis/{id:guid}")]
    public async Task<IActionResult> UpdateApheresisRecord(Guid id, UpdateApheresisRecordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await service.UpdateApheresisRecordAsync(id, dto);
            return result == null
                ? NotFound(new { message = "Record not found." })
                : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            await exceptionLogger.LogAsync(ex, nameof(UpdateApheresisRecord));
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("registers/apheresis/{id:guid}")]
    public async Task<IActionResult> DeleteApheresisRecord(Guid id)
    {
        var deleted = await service.DeleteApheresisRecordAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("registers/master-record")]
    public async Task<IActionResult> GetMasterRecords()
    {
        var records = await service.GetMasterRecordsAsync();
        return Ok(records);
    }
}
