namespace BloodBank.API.Services;

public interface IEmailOtpService
{
    Task<string> GenerateAndSendOtpAsync(string email, string purpose);
    Task<bool> VerifyOtpAsync(string email, string code, string purpose);
}
