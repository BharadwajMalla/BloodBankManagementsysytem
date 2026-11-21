using System.Data;
using System.Data.Common;
using Dapper;
using BloodBank.Application.Contracts;
using BloodBank.Infrastructure.Migrations;

namespace BloodBank.Infrastructure.Security;

public sealed class DapperDonorIdGenerator(Func<IDbConnection> createConnection) : IDonorIdGenerator
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        if (connection is DbConnection dbConnection)
            await dbConnection.OpenAsync(cancellationToken);
        else
            connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var nextValue = await connection.QuerySingleAsync<long>(
            new CommandDefinition(
                "SELECT NextValue FROM DonorIdSequences WHERE Id = 1",
                transaction: transaction,
                cancellationToken: cancellationToken));
        if (nextValue < 1)
            throw new InvalidOperationException("The donor ID sequence contains an invalid value.");

        var updated = await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE DonorIdSequences SET NextValue = @NextValue WHERE Id = 1 AND NextValue = @CurrentValue",
            new { NextValue = checked(nextValue + 1), CurrentValue = nextValue },
            transaction,
            cancellationToken: cancellationToken));
        if (updated != 1)
            throw new InvalidOperationException("The donor ID sequence could not be advanced.");

        transaction.Commit();
        return MasterDataSchemaInitializer.FormatDonorId(nextValue);
    }
}
