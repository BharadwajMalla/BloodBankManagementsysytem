using System.Net.Mail;
using Dapper;
using BloodBank.API.Data;
using BloodBank.API.Models;

namespace BloodBank.API.Services;

public class EmailOtpService : IEmailOtpService
{
    private readonly IDbConnectionFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailOtpService> _logger;

    public EmailOtpService(
        IDbConnectionFactory factory,
        IConfiguration configuration,
        ILogger<EmailOtpService> logger)
    {
        _factory = factory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateAndSendOtpAsync(string email, string purpose)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        using var conn = _factory.CreateConnection();

        // 1. Invalidate previous unused OTPs for this email and purpose via Dapper
        const string invalidateSql = @"
            UPDATE EmailOtps 
            SET IsUsed = 1 
            WHERE LOWER(Email) = LOWER(@Email) 
              AND Purpose = @Purpose 
              AND IsUsed = 0";
        await conn.ExecuteAsync(invalidateSql, new { Email = normalizedEmail, Purpose = purpose });

        // 2. Generate 6-digit numeric OTP
        var otpCode = Random.Shared.Next(100000, 999999).ToString();

        var otpRecord = new EmailOtp
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            OtpCode = otpCode,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        // 3. Insert OTP record via Dapper
        const string insertSql = @"
            INSERT INTO EmailOtps (Id, Email, OtpCode, Purpose, ExpiresAt, IsUsed, CreatedAt)
            VALUES (@Id, @Email, @OtpCode, @Purpose, @ExpiresAt, @IsUsed, @CreatedAt)";
        await conn.ExecuteAsync(insertSql, otpRecord);

        // 4. Attempt SMTP dispatch if configured
        var smtpHost = _configuration["Smtp:Host"];
        if (!string.IsNullOrEmpty(smtpHost))
        {
            try
            {
                var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
                var user = _configuration["Smtp:User"];
                var pass = _configuration["Smtp:Pass"];
                var from = _configuration["Smtp:From"] ?? "noreply@bloodbankA.org";
                var enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) ? ssl : true;

                using var client = new SmtpClient(smtpHost, port)
                {
                    EnableSsl = enableSsl
                };

                if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                {
                    client.Credentials = new System.Net.NetworkCredential(user, pass);
                }

                var subject = purpose == "Registration" ? "Your Blood Bank Verification Code" : "Your Password Reset OTP";
                var body = $@"Hello,

Your 6-digit verification code is: {otpCode}

This code will expire in 10 minutes. If you did not request this, please disregard this email.

Best regards,
Blood Bank Management System";

                var mail = new MailMessage(from, normalizedEmail, subject, body);
                await client.SendMailAsync(mail);
                _logger.LogInformation("Dispatched OTP email via SMTP to {Email}", normalizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send OTP email via SMTP. Falling back to log stream: {Message}", ex.Message);
            }
        }

        _logger.LogInformation("==================================================");
        _logger.LogInformation(">>> [EMAIL OTP DISPATCHED (DAPPER SQL)] <<<");
        _logger.LogInformation("Recipient: {Email} | Purpose: {Purpose} | Code: {Code}", normalizedEmail, purpose, otpCode);
        _logger.LogInformation("==================================================");

        return otpCode;
    }

    public async Task<bool> VerifyOtpAsync(string email, string code, string purpose)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var cleanCode = code.Trim();

        using var conn = _factory.CreateConnection();

        // Query active unexpired OTP via Dapper
        const string selectSql = @"
            SELECT * FROM EmailOtps
            WHERE LOWER(Email) = LOWER(@Email)
              AND OtpCode = @Code
              AND Purpose = @Purpose
              AND IsUsed = 0
              AND ExpiresAt >= @Now
            ORDER BY CreatedAt DESC";

        var otpRecord = await conn.QueryFirstOrDefaultAsync<EmailOtp>(selectSql, new
        {
            Email = normalizedEmail,
            Code = cleanCode,
            Purpose = purpose,
            Now = DateTime.UtcNow
        });

        if (otpRecord == null)
        {
            return false;
        }

        // Mark OTP as used via Dapper
        const string updateSql = "UPDATE EmailOtps SET IsUsed = 1 WHERE Id = @Id";
        await conn.ExecuteAsync(updateSql, new { Id = otpRecord.Id });
        return true;
    }
}
