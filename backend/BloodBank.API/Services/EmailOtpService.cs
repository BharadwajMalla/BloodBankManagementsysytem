using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using BloodBank.API.Data;
using BloodBank.API.Models;

namespace BloodBank.API.Services;

public class EmailOtpService : IEmailOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailOtpService> _logger;

    public EmailOtpService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<EmailOtpService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateAndSendOtpAsync(string email, string purpose)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // Invalidate previous unused OTPs for this email and purpose
        var oldOtps = await _context.EmailOtps
            .Where(o => o.Email == normalizedEmail && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();

        foreach (var old in oldOtps)
        {
            old.IsUsed = true;
        }

        // Generate 6-digit numeric OTP
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

        await _context.EmailOtps.AddAsync(otpRecord);
        await _context.SaveChangesAsync();

        // Attempt SMTP dispatch if configured
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
        _logger.LogInformation(">>> [EMAIL OTP DISPATCHED] <<<");
        _logger.LogInformation("Recipient: {Email} | Purpose: {Purpose} | Code: {Code}", normalizedEmail, purpose, otpCode);
        _logger.LogInformation("==================================================");

        return otpCode;
    }

    public async Task<bool> VerifyOtpAsync(string email, string code, string purpose)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var cleanCode = code.Trim();

        var otpRecord = await _context.EmailOtps
            .Where(o => o.Email == normalizedEmail && o.OtpCode == cleanCode && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRecord == null)
        {
            return false;
        }

        otpRecord.IsUsed = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
