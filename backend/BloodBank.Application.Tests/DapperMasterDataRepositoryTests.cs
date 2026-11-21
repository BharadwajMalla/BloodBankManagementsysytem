using Dapper;
using BloodBank.API.Data;
using BloodBank.Application.Dtos;
using BloodBank.Application.Models;
using BloodBank.Application.Services;
using BloodBank.Infrastructure.Migrations;
using BloodBank.Infrastructure.Repositories;
using BloodBank.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BloodBank.Application.Tests;

public sealed class DapperMasterDataRepositoryTests
{
    [Fact]
    public async Task MigrationBackfillsDonorIdsAndMasterDataSupportsSearchAuditAndStaffLogin()
    {
        var connectionString = $"Data Source=MasterData-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        await anchor.OpenAsync();
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(anchor)
            .Options;
        await using (var context = new ApplicationDbContext(options))
            await context.Database.EnsureCreatedAsync();

        var connectionFactory = new DbConnectionFactory(connectionString, isSqlite: true);
        var donorUserId = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        using (var seedConnection = connectionFactory.CreateConnection())
        {
            await seedConnection.ExecuteAsync(
                @"INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, IsEmailVerified, IsActive, CreatedAt)
                  VALUES (@Id, 'Existing Donor', 'existing@example.com', 'hash', 'Donor', 1, 1, @CreatedAt),
                         (@StaffId, 'Existing Tech', 'tech@example.com', 'hash', 'Technician', 1, 1, @CreatedAt)",
                new { Id = donorUserId, StaffId = staffUserId, CreatedAt = DateTime.UtcNow });
        }

        var initializer = new MasterDataSchemaInitializer(connectionFactory.CreateConnection, isSqlite: true);
        await initializer.InitializeAsync(CancellationToken.None);
        var repository = new DapperMasterDataRepository(connectionFactory.CreateConnection, isSqlite: true);
        var service = new MasterDataService(repository, new TestPasswordHasher());

        using (var connection = connectionFactory.CreateConnection())
        {
            var donorId = await connection.QuerySingleAsync<string>(
                "SELECT DonorId FROM Users WHERE Id = @Id",
                new { Id = donorUserId });
            Assert.Equal("UBC-D-000001", donorId);
        }
        var nextDonorId = await new DapperDonorIdGenerator(connectionFactory.CreateConnection)
            .GenerateAsync(CancellationToken.None);
        Assert.Equal("UBC-D-000002", nextDonorId);

        var product = await service.CreateAsync(
            MasterDataCategory.Product,
            new MasterDataCommand
            {
                Code = "RBC-01",
                Name = "Red Cell Product",
                Category = "Blood component",
                ShelfLifeDays = 42
            },
            "admin@example.com",
            CancellationToken.None);

        Assert.Equal(42, product.ShelfLifeDays);
        Assert.Equal("admin@example.com", product.CreatedBy);
        var searchedProducts = await service.GetAsync(
            MasterDataCategory.Product,
            "Red Cell",
            CancellationToken.None);
        Assert.Equal(product.Id, Assert.Single(searchedProducts).Id);

        var updatedProduct = await service.UpdateAsync(
            MasterDataCategory.Product,
            product.Id,
            new MasterDataCommand
            {
                Code = "RBC-01",
                Name = "Red Cell Product",
                Category = "Blood component",
                ShelfLifeDays = 35
            },
            "operator@example.com",
            CancellationToken.None);
        Assert.Equal(35, updatedProduct.ShelfLifeDays);
        Assert.Equal("operator@example.com", updatedProduct.UpdatedBy);

        var doctor = await service.CreateAsync(
            MasterDataCategory.Doctor,
            new MasterDataCommand
            {
                Code = "DOC-001",
                Name = "Dr Rao",
                RegistrationNumber = "MED-001",
                Specialty = "Haematology",
                HospitalName = "City Hospital"
            },
            "admin@example.com",
            CancellationToken.None);
        var savedDoctor = Assert.Single(
            await service.GetAsync(MasterDataCategory.Doctor, "MED-001", CancellationToken.None));
        Assert.Equal(doctor.Id, savedDoctor.Id);
        Assert.Equal("Haematology", savedDoctor.Specialty);
        await Assert.ThrowsAsync<MasterDataConflictException>(() => service.CreateAsync(
            MasterDataCategory.Doctor,
            new MasterDataCommand
            {
                Code = "DOC-002",
                Name = "Dr Another",
                RegistrationNumber = "MED-001"
            },
            "admin@example.com",
            CancellationToken.None));

        var bagType = await service.CreateAsync(
            MasterDataCategory.BagType,
            new MasterDataCommand { Code = "CPDA-450", Name = "CPDA 450 mL", VolumeMl = 450 },
            "admin@example.com",
            CancellationToken.None);
        Assert.Equal(450, bagType.VolumeMl);

        var supplier = await service.CreateAsync(
            MasterDataCategory.Supplier,
            new MasterDataCommand
            {
                Code = "SUP-001",
                Name = "Medical Supplies Ltd",
                ContactPerson = "Supply Desk",
                Phone = "9876543210"
            },
            "admin@example.com",
            CancellationToken.None);
        Assert.Equal("Supply Desk", supplier.ContactPerson);

        await service.CreateAsync(
            MasterDataCategory.Hospital,
            new MasterDataCommand
            {
                RegistrationNumber = "HOSP-001",
                Name = "City Hospital",
                ContactPerson = "Hospital Admin",
                Email = "hospital@example.com",
                Password = "Hospital@123"
            },
            "admin@example.com",
            CancellationToken.None);

        var staff = await service.GetAsync(MasterDataCategory.Staff, "Existing Tech", CancellationToken.None);
        Assert.Equal(staffUserId, Assert.Single(staff).Id);

        var newStaff = await service.CreateAsync(
            MasterDataCategory.Staff,
            new MasterDataCommand
            {
                Code = "TECH-001",
                Name = "New Technician",
                Email = "new.tech@example.com",
                Role = "Technician",
                Department = "Testing",
                Password = "Technician@123"
            },
            "admin@example.com",
            CancellationToken.None);
        Assert.Equal("Technician", newStaff.Role);

        await service.SetActiveAsync(
            MasterDataCategory.Staff,
            newStaff.Id,
            false,
            "admin@example.com",
            CancellationToken.None);

        using (var connection = connectionFactory.CreateConnection())
        {
            var isActive = await connection.QuerySingleAsync<bool>(
                "SELECT IsActive FROM Users WHERE Id = @Id",
                new { newStaff.Id });
            Assert.False(isActive);
            var sequenceValue = await connection.QuerySingleAsync<long>(
                "SELECT NextValue FROM DonorIdSequences WHERE Id = 1");
            Assert.Equal(3, sequenceValue);
        }
    }

    private sealed class TestPasswordHasher : BloodBank.Application.Contracts.IPasswordHasher
    {
        public string HashPassword(string password, int workFactor) => $"hashed:{password}";
    }
}
