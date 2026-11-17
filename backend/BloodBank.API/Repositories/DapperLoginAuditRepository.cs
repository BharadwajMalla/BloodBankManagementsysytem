using Dapper;
using BloodBank.API.Data;
using BloodBank.API.Models;

namespace BloodBank.API.Repositories;

public sealed class DapperLoginAuditRepository(IDbConnectionFactory connectionFactory) : ILoginAuditRepository
{
    public async Task AddAsync(LoginAuditRecord record, CancellationToken cancellationToken = default)
    {
        DapperTypeHandlers.Register(connectionFactory.IsSqlite);
        using var connection = connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO LoginAuditRecords
                (Id, UserId, Email, FullName, Role, LoginAt, Succeeded, IpAddress, UserAgent)
            VALUES
                (@Id, @UserId, @Email, @FullName, @Role, @LoginAt, @Succeeded, @IpAddress, @UserAgent)";

        await connection.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: cancellationToken));
    }

    public async Task<List<LoginAuditRecord>> GetAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken = default)
    {
        DapperTypeHandlers.Register(connectionFactory.IsSqlite);
        using var connection = connectionFactory.CreateConnection();
        var sql = connectionFactory.IsSqlite
            ? @"
                SELECT Id, UserId, Email, FullName, Role, LoginAt, Succeeded, IpAddress, UserAgent
                FROM LoginAuditRecords
                WHERE (@From IS NULL OR LoginAt >= @From)
                  AND (@To IS NULL OR LoginAt <= @To)
                ORDER BY LoginAt DESC, Id DESC
                LIMIT @Limit"
            : @"
                SELECT TOP (@Limit) Id, UserId, Email, FullName, Role, LoginAt, Succeeded, IpAddress, UserAgent
                FROM LoginAuditRecords
                WHERE (@From IS NULL OR LoginAt >= @From)
                  AND (@To IS NULL OR LoginAt <= @To)
                ORDER BY LoginAt DESC, Id DESC";

        var parameters = new
        {
            From = from?.UtcDateTime,
            To = to?.UtcDateTime,
            Limit = limit
        };

        var records = await connection.QueryAsync<LoginAuditRecord>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return records.AsList();
    }
}