using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BloodBank.API.DTOs;
using BloodBank.API.Models;
using BloodBank.API.Repositories;
using BloodBank.Application.Contracts;

namespace BloodBank.API.Services;

public class AuthService : IAuthService
{
    private readonly IBloodBankRepository _repo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailOtpService _otpService;
    private readonly IDonorIdGenerator _donorIdGenerator;

    public AuthService(
        IBloodBankRepository repo,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IEmailOtpService otpService,
        IDonorIdGenerator donorIdGenerator)
    {
        _repo = repo;
        _configuration = configuration;
        _logger = logger;
        _otpService = otpService;
        _donorIdGenerator = donorIdGenerator;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("JWT Secret Key is not configured.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "BloodBankSaaS";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "BloodBankClient";
        var expiryHours = int.TryParse(_configuration["Jwt:DurationInHours"], out var hours) ? hours : 24;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("name", user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expiryHours),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var role = dto.Role.Trim();

        if (role is not ("Donor" or "Hospital" or "Technician"))
        {
            throw new InvalidOperationException("Public registration is only available for Donor, Hospital, and Technician accounts.");
        }

        // 1. Verify 6-digit registration OTP
        var isOtpValid = await _otpService.VerifyOtpAsync(normalizedEmail, dto.OtpCode, "Registration");
        if (!isOtpValid)
        {
            throw new InvalidOperationException("Invalid or expired email verification code. Please enter the latest 6-digit code sent to your email.");
        }

        var existingUser = await _repo.FindUserByEmailAsync(normalizedEmail);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"A user with email '{dto.Email}' already exists.");
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            DonorId = role == "Donor"
                ? await _donorIdGenerator.GenerateAsync(cancellationToken)
                : null,
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = HashPassword(dto.Password),
            Role = role,
            Phone = dto.Phone,
            BloodGroup = dto.BloodGroup,
            HospitalName = dto.HospitalName,
            HospitalRegNumber = dto.HospitalRegNumber,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = normalizedEmail
        };

        await _repo.AddUserAsync(newUser);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("New user registered and email verified: {Email} (Role: {Role})", newUser.Email, newUser.Role);

        var token = GenerateJwtToken(newUser);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = MapToUserDto(newUser)
        };
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var user = await _repo.FindUserByEmailAsync(normalizedEmail);
        if (user == null)
        {
            throw new InvalidOperationException("No registered account found with that email address.");
        }

        var isOtpValid = await _otpService.VerifyOtpAsync(normalizedEmail, dto.OtpCode, "PasswordReset");
        if (!isOtpValid)
        {
            throw new InvalidOperationException("Invalid or expired verification OTP code. Please request a fresh OTP.");
        }

        user.PasswordHash = HashPassword(dto.NewPassword);
        user.IsEmailVerified = true;
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for user: {Email}", normalizedEmail);
        return true;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var user = await _repo.FindUserByEmailAsync(normalizedEmail);

        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("This account has been deactivated.");
        }
        if (!user.IsEmailVerified)
        {
            throw new UnauthorizedAccessException("Email address is not verified.");
        }

        _logger.LogInformation("User logged in: {Email} (Role: {Role})", user.Email, user.Role);

        var token = GenerateJwtToken(user);
        var expiryHours = int.TryParse(_configuration["Jwt:DurationInHours"], out var hours) ? hours : 24;

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            User = MapToUserDto(user)
        };
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            DonorId = user.DonorId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Phone = user.Phone,
            BloodGroup = user.BloodGroup,
            HospitalName = user.HospitalName,
            HospitalRegNumber = user.HospitalRegNumber,
            CreatedAt = user.CreatedAt
        };
    }
}
