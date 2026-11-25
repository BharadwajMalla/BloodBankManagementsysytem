using BloodBank.API.DTOs;
using BloodBank.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,BloodBank,TechnicalSupervisor,Technician")]
public sealed class ChatController(IInventoryChatService chatService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChatResponseDto>> Ask(
        [FromBody] ChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.History is null || request.History.Any(turn => turn.Role is not ("user" or "assistant")))
            return BadRequest(new { message = "Chat history can contain only user and assistant turns." });

        var response = await chatService.AskAsync(request, cancellationToken);
        if (response is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The AI assistant is temporarily unavailable.",
                Detail = "Gemini is experiencing high demand. Please try again later."
            });
        }

        return Ok(response);
    }
}
