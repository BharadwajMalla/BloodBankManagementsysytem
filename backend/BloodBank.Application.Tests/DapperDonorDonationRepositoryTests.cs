using BloodBank.API.Data;
using BloodBank.Application.Models;
using BloodBank.Infrastructure.Migrations;
using BloodBank.Infrastructure.Repositories;
using BloodBank.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BloodBank.API.Models;
using BloodBank.API.Repositories;
using Dapper;
using Xunit;

namespace BloodBank.Application.Tests;

public sealed class DapperDonorDonationRepositoryTests
{
    [Fact]
    public async Task RepositoryPersistsDonorAndDonationUsingExistingSqliteSchema()
    {
        var connectionString = $"Data Source=DonorSlice-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(anchor)
            .Options;
        await using (var context = new ApplicationDbContext(options))
            await context.Database.EnsureCreatedAsync();

        var connectionFactory = new DbConnectionFactory(connectionString, isSqlite: true);
        var schemaInitializer = new MasterDataSchemaInitializer(connectionFactory.CreateConnection, isSqlite: true);
        await schemaInitializer.InitializeAsync(CancellationToken.None);
        var repository = new DapperDonorDonationRepository(
            connectionFactory.CreateConnection,
            new DapperDonorIdGenerator(connectionFactory.CreateConnection));
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Smith",
            Email = "jane@example.com",
            Phone = "5550100",
            DateOfBirth = DateTime.UtcNow.Date.AddYears(-30),
            Sex = "Female",
            Address = "Address",
            BloodGroup = "O+",
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddDonorAsync(donor, "hashed-password", CancellationToken.None);
        await repository.AddDonationAsync(
            new Donation
            {
                DonorId = donor.Id,
                DonorName = donor.FullName,
                BloodGroup = donor.BloodGroup!,
                DonationDate = DateTime.UtcNow,
                Hemoglobin = 13.2m
            },
            CancellationToken.None);

        var loadedDonor = await repository.FindDonorAsync(donor.Id, CancellationToken.None);
        var loadedDonations = await repository.GetDonationsForDonorAsync(donor.Id, CancellationToken.None);

        Assert.NotNull(loadedDonor);
        Assert.Equal(donor.Email, loadedDonor.Email);
        Assert.Equal("UBC-D-000001", loadedDonor.DonorId);
        var loadedDonation = Assert.Single(loadedDonations);
        Assert.Equal(donor.Id, loadedDonation.DonorId);
        Assert.Equal(13.2m, loadedDonation.Hemoglobin);
    }

    [Fact]
    public async Task DeactivatingDonorHidesAccountButRetainsDonationHistory()
    {
        var connectionString = $"Data Source=DeactivateDonor-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(anchor).Options;
        await using (var context = new ApplicationDbContext(options))
            await context.Database.EnsureCreatedAsync();
        var factory = new DbConnectionFactory(connectionString, isSqlite: true);
        await new MasterDataSchemaInitializer(factory.CreateConnection, true).InitializeAsync(CancellationToken.None);
        var repository = new DapperDonorDonationRepository(
            factory.CreateConnection,
            new DapperDonorIdGenerator(factory.CreateConnection));
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Smith",
            Email = "jane@example.com",
            DateOfBirth = DateTime.UtcNow.Date.AddYears(-30),
            BloodGroup = "O+",
            CreatedAt = DateTime.UtcNow
        };
        await repository.AddDonorAsync(donor, "hashed-password", CancellationToken.None);
        await repository.AddDonationAsync(
            new Donation { DonorId = donor.Id, DonorName = donor.FullName, BloodGroup = "O+", DonationDate = DateTime.UtcNow },
            CancellationToken.None);

        var deactivated = await repository.DeactivateDonorAsync(donor.Id, "staff", CancellationToken.None);

        Assert.True(deactivated);
        Assert.Empty(await repository.GetDonorsAsync(CancellationToken.None));
        Assert.Null(await repository.FindDonorAsync(donor.Id, CancellationToken.None));
        Assert.Single(await repository.GetDonationsForDonorAsync(donor.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DeletePendingRequestPreservesProcessedRequests()
    {
        var connectionString = $"Data Source=DeleteRequest-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        await anchor.ExecuteAsync(@"
            CREATE TABLE BloodRequests (
                Id TEXT PRIMARY KEY,
                Status TEXT NOT NULL,
                ProcessedAt TEXT NULL,
                IssuedBagNumbers TEXT NULL,
                PaymentStatus TEXT NOT NULL
            )");
        var factory = new DbConnectionFactory(connectionString, isSqlite: true);
        var repository = new DapperBloodBankRepository(factory);
        var pendingId = Guid.NewGuid();
        var processedId = Guid.NewGuid();
        await anchor.ExecuteAsync(@"
            INSERT INTO BloodRequests (Id, Status, ProcessedAt, IssuedBagNumbers, PaymentStatus)
            VALUES (@PendingId, 'Pending', NULL, NULL, 'Pending'),
                   (@ProcessedId, 'Approved', @ProcessedAt, NULL, 'Pending')",
            new { PendingId = pendingId, ProcessedId = processedId, ProcessedAt = DateTime.UtcNow });

        var pendingDeleted = await repository.DeletePendingRequestAsync(pendingId);
        var processedDeleted = await repository.DeletePendingRequestAsync(processedId);

        Assert.True(pendingDeleted);
        Assert.False(processedDeleted);
        Assert.Equal(1, await anchor.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM BloodRequests WHERE Id = @Id",
            new { Id = processedId }));
    }

    [Fact]
    public async Task DonationAndBloodBagShareGeneratedOrSuppliedBagNumber()
    {
        var connectionString = $"Data Source=DonationBag-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(anchor).Options;
        await using (var context = new ApplicationDbContext(options))
            await context.Database.EnsureCreatedAsync();
        var factory = new DbConnectionFactory(connectionString, isSqlite: true);
        await new MasterDataSchemaInitializer(factory.CreateConnection, true).InitializeAsync(CancellationToken.None);
        var repository = new DapperDonorDonationRepository(
            factory.CreateConnection,
            new DapperDonorIdGenerator(factory.CreateConnection));
        var donation = new Donation
        {
            DonorId = Guid.NewGuid(),
            DonorName = "Jane Smith",
            BloodGroup = "O+",
            DonationDate = DateTime.UtcNow,
            BagNumber = "UBC-BAG-TEST-0001",
            CpdaBatchNumber = "CPDA-TEST-001",
            DonationCategory = "Replacement",
            HbTestMethod = "Hemocue",
            HbEligibilityStatus = "Fit",
            DeferralRecords = "None"
        };

        await repository.AddDonationAsync(donation, CancellationToken.None);

        using var connection = factory.CreateConnection();
        var loadedDonationBag = await connection.QuerySingleAsync<string>(
            "SELECT BagNumber FROM DonationRecords WHERE Id = @Id",
            new { donation.Id });
        var bag = await connection.QuerySingleAsync<(string BagNumber, string? CpdaBatchNumber, string Status, string ScreeningStatus)>(
            "SELECT BagNumber, CpdaBatchNumber, Status, ScreeningStatus FROM BloodBags WHERE BagNumber = @BagNumber",
            new { BagNumber = loadedDonationBag });
        Assert.Equal("UBC-BAG-TEST-0001", loadedDonationBag);
        Assert.Equal("UBC-BAG-TEST-0001", bag.BagNumber);
        Assert.Equal("CPDA-TEST-001", bag.CpdaBatchNumber);
        Assert.Equal("Available", bag.Status);
        Assert.Equal("Unscreened", bag.ScreeningStatus);
    }

    [Fact]
    public async Task SqliteMigrationAddsExpandedColumnsAndBackfillsBagExpiry()
    {
        var connectionString = $"Data Source=Migration-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        await anchor.ExecuteAsync(@"
            CREATE TABLE Users (Id TEXT PRIMARY KEY, Email TEXT, Role TEXT, CreatedAt TEXT);
            CREATE TABLE BloodBags (Id TEXT PRIMARY KEY, BagNumber TEXT, ComponentType TEXT, AddedDate TEXT, IsIssued INTEGER);
            CREATE TABLE DonationRecords (Id TEXT PRIMARY KEY);
            CREATE TABLE TtiScreeningRecords (Id TEXT PRIMARY KEY);
            CREATE TABLE BloodRequests (Id TEXT PRIMARY KEY);
            CREATE TABLE ApheresisRecords (Id TEXT PRIMARY KEY);
            INSERT INTO BloodBags (Id, BagNumber, ComponentType, AddedDate, IsIssued)
            VALUES (@Id, 'OLD-BAG', 'Platelets', @Date, 0)",
            new { Id = Guid.NewGuid().ToString(), Date = DateTime.UtcNow.AddDays(-1).ToString("O") });

        var factory = new DbConnectionFactory(connectionString, isSqlite: true);
        await new MasterDataSchemaInitializer(factory.CreateConnection, true).InitializeAsync(CancellationToken.None);

        using var connection = factory.CreateConnection();
        var donationColumns = (await connection.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('DonationRecords')")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bagColumns = (await connection.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('BloodBags')")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ttiColumns = (await connection.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('TtiScreeningRecords')")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requestColumns = (await connection.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('BloodRequests')")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("CpdaBatchNumber", donationColumns);
        Assert.Contains("CpdaBatchNumber", bagColumns);
        Assert.Contains("ExpiryDate", bagColumns);
        Assert.Contains("HivIResult", ttiColumns);
        Assert.Contains("ElisaKitBatch", ttiColumns);
        Assert.Contains("HbGdl", requestColumns);
        Assert.Contains("IssueSerial", requestColumns);
        var expiry = await connection.QuerySingleAsync<DateTime?>(
            "SELECT ExpiryDate FROM BloodBags WHERE BagNumber = 'OLD-BAG'");
        Assert.True(expiry.HasValue);
    }

    [Fact]
    public async Task AtomicIssueFailsClosedForMissingExpiryMissingTtiAndReactiveTti()
    {
        var connectionString = $"Data Source=IssueRules-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(anchor).Options;
        await using (var context = new ApplicationDbContext(options))
            await context.Database.EnsureCreatedAsync();
        var factory = new DbConnectionFactory(connectionString, isSqlite: true);
        await new MasterDataSchemaInitializer(factory.CreateConnection, true).InitializeAsync(CancellationToken.None);

        using var connection = factory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO BloodStocks (Id,BloodGroup,ComponentType,QuantityUnits,BloodBankName,LastUpdated)
            VALUES (@Id,'O+','Whole Blood',3,'Blood Bank A',@Now)",
            new { Id = Guid.NewGuid(), Now = DateTime.UtcNow });

        foreach (var bag in new[] { "NO-EXPIRY", "NO-TTI", "REACTIVE" })
        {
            await connection.ExecuteAsync(@"
                INSERT INTO BloodBags (Id,BagNumber,BloodGroup,ComponentType,QuantityUnits,AddedDate,IsIssued,Status,ExpiryDate,ScreeningStatus)
                VALUES (@Id,@Bag,@Group,'Whole Blood',1,@Now,0,'Available',@Expiry,'Unscreened')",
                new
                {
                    Id = Guid.NewGuid(),
                    Bag = bag,
                    Group = "O+",
                    Now = DateTime.UtcNow,
                    Expiry = bag == "NO-EXPIRY" ? null : (object)DateTime.UtcNow.AddDays(20)
                });
        }
        await connection.ExecuteAsync(@"
            INSERT INTO TtiScreeningRecords
            (Id,BagBarcode,BloodGroup,TestingDate,HivResult,HivIResult,HivIiResult,HbsAgResult,HcvResult,VdrlResult,
             MalariaResult,AtypicalAntibodies,OverallStatus,TestedBy,CreatedAt)
            VALUES
            (@Id,'REACTIVE','O+',@Now,'Reactive','Non-Reactive','Non-Reactive','Non-Reactive','Non-Reactive','Non-Reactive',
             'Negative','Negative','Failed - Reactive','Lab',@Now)",
            new { Id = Guid.NewGuid(), Now = DateTime.UtcNow });

        var repository = new DapperBloodBankRepository(factory);
        var result = await repository.TryIssueBagsAtomicallyAsync("O+", "Whole Blood", 1, "Staff");

        Assert.True(result.Error is not null);
        Assert.Empty(result.BagNumbers);
    }
}
