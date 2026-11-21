using System.Data;
using System.Data.Common;
using System.Reflection;
using Dapper;

namespace BloodBank.Infrastructure.Migrations;

public sealed class MasterDataSchemaInitializer(
    Func<IDbConnection> createConnection,
    bool isSqlite)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        if (connection is not DbConnection dbConnection)
            throw new InvalidOperationException("The master-data connection must support asynchronous I/O.");

        await dbConnection.OpenAsync(cancellationToken);

        if (isSqlite)
        {
            await EnsureSqliteUserColumnsAsync(connection, cancellationToken);
            await EnsureSqliteOperationalColumnsAsync(connection, cancellationToken);
            await ExecuteEmbeddedScriptAsync(connection, "MasterData.Sqlite.sql", cancellationToken);
        }
        else
        {
            await ExecuteEmbeddedScriptAsync(connection, "MasterData.SqlServer.sql", cancellationToken);
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Users SET CreatedBy = 'System' WHERE CreatedBy IS NULL",
            cancellationToken: cancellationToken));
        await BackfillDonorIdsAsync(connection, cancellationToken);
        await BackfillStaffProfilesAsync(connection, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            isSqlite
                ? "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_DonorId ON Users(DonorId) WHERE DonorId IS NOT NULL"
                : "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_DonorId' AND object_id = OBJECT_ID('Users')) CREATE UNIQUE INDEX IX_Users_DonorId ON Users(DonorId) WHERE DonorId IS NOT NULL",
            cancellationToken: cancellationToken));
    }

    private async Task EnsureSqliteUserColumnsAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        var columns = await connection.QueryAsync<SqliteColumn>(
            new CommandDefinition("PRAGMA table_info('Users')", cancellationToken: cancellationToken));
        var existing = columns.Select(column => column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var additions = new[]
        {
            ("DonorId", "TEXT NULL"),
            ("IsActive", "INTEGER NOT NULL DEFAULT 1"),
            ("UpdatedAt", "TEXT NULL"),
            ("CreatedBy", "TEXT NULL"),
            ("UpdatedBy", "TEXT NULL")
        };

        foreach (var (name, type) in additions)
        {
            if (!existing.Contains(name))
                await connection.ExecuteAsync(new CommandDefinition(
                    $"ALTER TABLE Users ADD COLUMN {name} {type}",
                    cancellationToken: cancellationToken));
        }
    }

    private async Task EnsureSqliteOperationalColumnsAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        await EnsureColumnsAsync(connection, "BloodBags",
        [
            ("Status", "TEXT NOT NULL DEFAULT 'Available'"),
            ("ExpiryDate", "TEXT NULL"),
            ("ScreeningStatus", "TEXT NOT NULL DEFAULT 'Unscreened'"),
            ("ReservedUntil", "TEXT NULL"),
            ("IssuedBy", "TEXT NULL"),
            ("IssuedAt", "TEXT NULL"),
            ("CpdaBatchNumber", "TEXT NULL")
        ], cancellationToken);

        await EnsureColumnsAsync(connection, "ApheresisRecords",
        [
            ("YearlySlNo", "INTEGER NULL"),
            ("MonthlySlNo", "INTEGER NULL")
        ], cancellationToken);

        await EnsureColumnsAsync(connection, "DonationRecords",
        [
            ("YearlySlNo","INTEGER NULL"),("MonthlySlNo","INTEGER NULL"),
            ("DonationCategory","TEXT NOT NULL DEFAULT 'Voluntary'"),
            ("ReplacementPatientName","TEXT NULL"),("ReplacementPatientHospital","TEXT NULL"),
            ("ReplacementPatientIpNo","TEXT NULL"),("BagNumber","TEXT NULL"),
            ("HbTestMethod","TEXT NULL"),("HbEligibilityStatus","TEXT NULL"),("DeferralRecords","TEXT NULL"),
            ("CpdaBatchNumber","TEXT NULL"),
            ("WeightKg","REAL NULL"),("PulsePerMinute","INTEGER NULL"),("Hemoglobin","REAL NULL"),
            ("Hematocrit","REAL NULL"),("PlateletCount","INTEGER NULL"),("BloodPressure","TEXT NULL"),
            ("TemperatureCelsius","REAL NULL"),("ScreeningStaffName","TEXT NULL"),("ScreeningStaffSignature","TEXT NULL"),
            ("ScreeningDate","TEXT NULL"),("ScreeningTime","TEXT NULL"),("MedicalExamAccepted","INTEGER NOT NULL DEFAULT 1"),
            ("DeferralReason","TEXT NULL"),("MedicalOfficerName","TEXT NULL"),("MedicalOfficerSignature","TEXT NULL"),
            ("MedicalOfficerDate","TEXT NULL"),("DonorNumber","TEXT NULL"),("BagType","TEXT NULL"),
            ("SegmentNumber","TEXT NULL"),("DonationStartTime","TEXT NULL"),("DonationEndTime","TEXT NULL"),
            ("VenipunctureSite","TEXT NULL"),("PhlebotomistName","TEXT NULL"),("PhlebotomistSignature","TEXT NULL"),
            ("VisualInspectionPassed","INTEGER NOT NULL DEFAULT 1"),("DonorConfirmed","INTEGER NOT NULL DEFAULT 1"),
            ("FeelsHealthyToday","INTEGER NOT NULL DEFAULT 1"),("DonatedRecently","INTEGER NOT NULL DEFAULT 0"),
            ("HadDonationDiscomfort","INTEGER NOT NULL DEFAULT 0"),("AteRecently","INTEGER NOT NULL DEFAULT 1"),
            ("SleptWell","INTEGER NOT NULL DEFAULT 1"),("HasInfectionRisk","INTEGER NOT NULL DEFAULT 0"),
            ("HasColdFeverOrSinusitis","INTEGER NOT NULL DEFAULT 0"),("TookAntibioticsRecently","INTEGER NOT NULL DEFAULT 0"),
            ("TookAlcoholRecently","INTEGER NOT NULL DEFAULT 0"),("SmokesFrequently","INTEGER NOT NULL DEFAULT 0"),
            ("TookMedicationRecently","INTEGER NOT NULL DEFAULT 0"),("HadRecentVaccination","INTEGER NOT NULL DEFAULT 0"),
            ("HadRecentIllness","INTEGER NOT NULL DEFAULT 0"),("HadRecentSurgeryOrTattoo","INTEGER NOT NULL DEFAULT 0"),
            ("HadRecentMalariaOrEndemicExposure","INTEGER NOT NULL DEFAULT 0"),("HasChronicDisease","INTEGER NOT NULL DEFAULT 0"),
            ("IsPregnantOrBreastfeeding","INTEGER NOT NULL DEFAULT 0"),("HadRecentAbortion","INTEGER NOT NULL DEFAULT 0"),
            ("HasUnsafeSexualExposure","INTEGER NOT NULL DEFAULT 0"),("HasHivOrHepatitisRisk","INTEGER NOT NULL DEFAULT 0"),
            ("HasPermanentDisqualification","INTEGER NOT NULL DEFAULT 0")
        ], cancellationToken);

        await EnsureColumnsAsync(connection, "TtiScreeningRecords",
        [
            ("HivIResult","TEXT NOT NULL DEFAULT 'Non-Reactive'"),
            ("HivIiResult","TEXT NOT NULL DEFAULT 'Non-Reactive'"),
            ("ElisaKitBatch","TEXT NULL"),("ElisaSignOffBy","TEXT NULL"),("ElisaSignOffAt","TEXT NULL")
        ], cancellationToken);

        await EnsureColumnsAsync(connection, "BloodRequests",
        [
            ("Indication","TEXT NULL"),("EmergencyUrgency","TEXT NULL"),("RefDoctor","TEXT NULL"),
            ("IpNumber","TEXT NULL"),("RoomNumber","TEXT NULL"),("PreviousTransfusion","INTEGER NULL"),
            ("TransfusionReactionHistory","INTEGER NULL"),("CrossMatchingReport","TEXT NULL"),("BillNumber","TEXT NULL"),
            ("YearlySlNo","INTEGER NULL"),("MonthlySlNo","INTEGER NULL"),("CrossMatchingMethod","TEXT NULL"),
            ("HbGdl","REAL NULL"),("PcvPercent","REAL NULL"),("PlateletCount","REAL NULL"),
            ("IndicationDiagnosis","TEXT NULL"),("RequiredResponseTime","TEXT NULL"),("RequiredDateTime","TEXT NULL"),
            ("IssueOrReserve","TEXT NULL"),("ConsentTakenBy","TEXT NULL"),("ConsentDate","TEXT NULL"),
            ("ConsentForIssue","INTEGER NOT NULL DEFAULT 0"),("BloodBankUseOnly","INTEGER NOT NULL DEFAULT 0"),
            ("IssueSerial","TEXT NULL"),("ReceiverName","TEXT NULL"),("IssuedByName","TEXT NULL"),
            ("BloodBankOfficerSignOff","TEXT NULL")
        ], cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(@"
            UPDATE BloodBags
            SET ExpiryDate = CASE
              WHEN LOWER(COALESCE(ComponentType,'')) LIKE '%platelet%' THEN datetime(COALESCE(AddedDate, CURRENT_TIMESTAMP), '+5 days')
              WHEN LOWER(COALESCE(ComponentType,'')) LIKE '%whole%' THEN datetime(COALESCE(AddedDate, CURRENT_TIMESTAMP), '+35 days')
              WHEN LOWER(COALESCE(ComponentType,'')) LIKE '%plasma%' THEN datetime(COALESCE(AddedDate, CURRENT_TIMESTAMP), '+365 days')
              ELSE datetime(COALESCE(AddedDate, CURRENT_TIMESTAMP), '+42 days')
            END
            WHERE ExpiryDate IS NULL", cancellationToken: cancellationToken));
    }

    private static async Task EnsureColumnsAsync(
        IDbConnection connection,
        string tableName,
        IEnumerable<(string Name, string Type)> additions,
        CancellationToken cancellationToken)
    {
        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@TableName",
                new { TableName = tableName },
                cancellationToken: cancellationToken));
        if (exists == 0)
            return;
        var columns = await connection.QueryAsync<SqliteColumn>(
            new CommandDefinition($"PRAGMA table_info('{tableName}')", cancellationToken: cancellationToken));
        var existing = columns.Select(column => column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, type) in additions)
        {
            if (!existing.Contains(name))
                await connection.ExecuteAsync(new CommandDefinition(
                    $"ALTER TABLE {tableName} ADD COLUMN {name} {type}",
                    cancellationToken: cancellationToken));
        }
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

    private sealed class SqliteColumn
    {
        public string Name { get; set; } = string.Empty;
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
