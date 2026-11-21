using BloodBank.API.Models;

namespace BloodBank.API.Repositories;

public interface ILoginAuditRepository
{
    Task AddAsync(LoginAuditRecord record, CancellationToken cancellationToken = default);

    Task<List<LoginAuditRecord>> GetAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken = default);
}