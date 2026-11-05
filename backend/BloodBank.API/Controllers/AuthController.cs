using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodBank.API.DTOs;
using BloodBank.API.Repositories;
using BloodBank.API.Services;

namespace BloodBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailOtpService _otpService;
    private readonly IBloodBankRepository _repo;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IEmailOtpService otpService,
        IBloodBankRepository repo,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _otpService = otpService;
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// Send 6-digit email OTP for new user registration
    /// </summary>
    [HttpPost("send-registration-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendRegistrationOtp([FromBody] SendOtpDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var existingUser = await _repo.FindUserByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                return Conflict(new { message = $"An account with email '{dto.Email}' already exists." });
            }

            var otp = await _otpService.GenerateAndSendOtpAsync(normalizedEmail, "Registration");

            return Ok(new
            {
                message = $"Verification code sent to {dto.Email}.",
                previewOtp = otp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration OTP to {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to dispatch verification OTP: " + ex.Message });
        }
    }

    /// <summary>
    /// Register a new user with role Admin, Hospital, or Donor
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.RegisterAsync(dto);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { 
                message = "Registration failed: " + ex.Message,
                detail = ex.InnerException?.Message 
            });
        }
    }

    /// <summary>
    /// Authenticate user credentials and issue signed JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { 
                message = "Authentication failed: " + ex.Message,
                detail = ex.InnerException?.Message 
            });
        }
    }

    /// <summary>
    /// Send 6-digit email OTP for Forgot Password
    /// </summary>
    [HttpPost("forgot-password/send-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendForgotPasswordOtp([FromBody] SendOtpDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var user = await _repo.FindUserByEmailAsync(normalizedEmail);
            if (user == null)
            {
                return NotFound(new { message = $"No account found with email address '{dto.Email}'." });
            }

            var otp = await _otpService.GenerateAndSendOtpAsync(normalizedEmail, "PasswordReset");

            return Ok(new
            {
                message = $"Password reset OTP sent to {dto.Email}.",
                previewOtp = otp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP to {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to dispatch password reset OTP: " + ex.Message });
        }
    }

    /// <summary>
    /// Reset password using email, 6-digit OTP, and new password
    /// </summary>
    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Your password has been successfully reset. You can now log in with your new password." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed for {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Password reset failed: " + ex.Message });
        }
    }
}
