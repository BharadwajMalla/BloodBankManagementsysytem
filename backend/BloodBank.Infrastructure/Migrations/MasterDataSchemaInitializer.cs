using System.Data;
using System.Data.Common;
using System.Reflection;
using Dapper;

namespace BloodBank.Infrastructure.Migrations;

public sealed class MasterDataSchemaInitializer(Func<IDbConnection> createConnection)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        if (connection is not DbConnection dbConnection)
            throw new InvalidOperationException("The master-data connection must support asynchronous I/O.");

        await dbConnection.OpenAsync(cancellationToken);
        await ExecuteEmbeddedScriptAsync(connection, "MasterData.SqlServer.sql", cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Users SET CreatedBy = 'System' WHERE CreatedBy IS NULL",
            cancellationToken: cancellationToken));
        await BackfillDonorIdsAsync(connection, cancellationToken);
        await BackfillStaffProfilesAsync(connection, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            @"IF NOT EXISTS (
                  SELECT 1 FROM sys.indexes
                  WHERE name = 'IX_Users_DonorId' AND object_id = OBJECT_ID('Users')
              )
              CREATE UNIQUE INDEX IX_Users_DonorId ON Users(DonorId) WHERE DonorId IS NOT NULL",
            cancellationToken: cancellationToken));
    }

    private static async Task ExecuteEmbeddedScriptAsync(
        IDbConnection connection,
        string fileName,
        CancellationToken cancellationToken)
    {
        var resourceName = $"BloodBank.Infrastructure.Migrations.{fileName}";
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded migration script '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task BackfillDonorIdsAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var nextValue = await connection.QuerySingleAsync<long>(
            new CommandDefinition(
                "SELECT NextValue FROM DonorIdSequences WHERE Id = 1",
                transaction: transaction,
                cancellationToken: cancellationToken));
        var currentIds = await connection.QueryAsync<string>(
            new CommandDefinition(
                "SELECT DonorId FROM Users WHERE Role = 'Donor' AND DonorId IS NOT NULL",
                transaction: transaction,
                cancellationToken: cancellationToken));
        var maxAssigned = currentIds
            .Select(ParseDonorNumber)
            .DefaultIfEmpty(0)
            .Max();
        nextValue = Math.Max(nextValue, maxAssigned + 1);

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE DonorIdSequences SET NextValue = @NextValue WHERE Id = 1",
            new { NextValue = nextValue },
            transaction,
            cancellationToken: cancellationToken));

        var missing = await connection.QueryAsync<MissingDonorId>(
            new CommandDefinition(
                "SELECT Id, Email FROM Users WHERE Role = 'Donor' AND DonorId IS NULL ORDER BY CreatedAt, Id",
                transaction: transaction,
                cancellationToken: cancellationToken));

        foreach (var donor in missing)
        {
            var donorId = FormatDonorId(nextValue++);
            await connection.ExecuteAsync(new CommandDefinition(
                @"UPDATE Users
                  SET DonorId = @DonorId,
                      CreatedBy = COALESCE(CreatedBy, @CreatedBy)
                  WHERE Id = @Id AND Role = 'Donor' AND DonorId IS NULL",
                new { DonorId = donorId, CreatedBy = donor.Email, donor.Id },
                transaction,
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE DonorIdSequences SET NextValue = @NextValue WHERE Id = 1",
            new { NextValue = nextValue },
            transaction,
            cancellationToken: cancellationToken));
        transaction.Commit();
    }

    private static async Task BackfillStaffProfilesAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var missing = await connection.QueryAsync<StaffProfileSeed>(
            new CommandDefinition(
                @"SELECT u.Id, u.CreatedAt, u.CreatedBy, u.Role
                  FROM Users u
                  LEFT JOIN StaffMembers s ON s.UserId = u.Id
                  WHERE u.Role IN ('Admin', 'BloodBank', 'TechnicalSupervisor', 'Technician')
                    AND s.UserId IS NULL
                  ORDER BY u.CreatedAt, u.Id",
                transaction: transaction,
                cancellationToken: cancellationToken));

        foreach (var user in missing)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                @"INSERT INTO StaffMembers
                    (Id, UserId, EmployeeNumber, Department, IsActive, CreatedAt, CreatedBy)
                  VALUES
                    (@Id, @Id, @EmployeeNumber, 'Blood Bank', 1, @CreatedAt, @CreatedBy)",
                new
                {
                    user.Id,
                    EmployeeNumber = $"STAFF-{user.Id:N}",
                    user.CreatedAt,
                    CreatedBy = user.CreatedBy ?? "System"
                },
                transaction,
                cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    public static string FormatDonorId(long sequence) => $"UBC-D-{sequence:D6}";

    private static long ParseDonorNumber(string donorId)
    {
        const string prefix = "UBC-D-";
        return donorId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && long.TryParse(donorId.AsSpan(prefix.Length), out var number)
                ? number
                : 0;
    }

    private sealed class MissingDonorId
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    private sealed class StaffProfileSeed
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
