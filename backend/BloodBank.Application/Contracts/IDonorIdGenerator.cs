namespace BloodBank.Application.Contracts;

public interface IDonorIdGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken);
}
