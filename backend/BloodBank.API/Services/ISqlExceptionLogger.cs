namespace BloodBank.API.Services;

public interface ISqlExceptionLogger
{
    Task LogAsync(Exception exception, string context);
}
