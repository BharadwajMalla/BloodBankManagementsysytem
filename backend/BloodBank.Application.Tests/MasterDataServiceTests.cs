using BloodBank.Application.Contracts;
using BloodBank.Application.Dtos;
using BloodBank.Application.Models;
using BloodBank.Application.Services;
using Xunit;

namespace BloodBank.Application.Tests;

public sealed class MasterDataServiceTests
{
    [Fact]
    public async Task CreateAsyncRejectsInvalidShelfLifeStaffRoleAndInitialPasswordBeforeRepositoryAccess()
    {
        var repository = new EmptyMasterDataRepository();
        var service = new MasterDataService(repository, new TestPasswordHasher());
        var invalidCommands = new[]
        {
            (MasterDataCategory.Product, new MasterDataCommand
            {
                Code = "PRODUCT-1",
                Name = "Product",
                ShelfLifeDays = 0
            }),
            (MasterDataCategory.Staff, new MasterDataCommand
            {
                Code = "STAFF-1",
                Name = "Staff",
                Email = "staff@example.com",
                Role = "Unsupported",
                Department = "Testing",
                Password = "Strong@123"
            }),
            (MasterDataCategory.Staff, new MasterDataCommand
            {
                Code = "STAFF-2",
                Name = "Staff",
                Email = "staff@example.com",
                Role = "Technician",
                Department = "Testing",
                Password = "weak"
            })
        };

        foreach (var (category, command) in invalidCommands)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
                category,
                command,
                "admin@example.com",
                CancellationToken.None));
        }

        Assert.False(repository.WasAccessed);
    }

    private sealed class EmptyMasterDataRepository : IMasterDataRepository
    {
        public bool WasAccessed { get; private set; }

        public Task<IReadOnlyList<MasterDataEntry>> GetAsync(
            MasterDataCategory category,
            string? search,
            CancellationToken cancellationToken)
        {
            WasAccessed = true;
            return Task.FromResult<IReadOnlyList<MasterDataEntry>>([]);
        }

        public Task<MasterDataEntry?> FindAsync(
            MasterDataCategory category,
            Guid id,
            CancellationToken cancellationToken)
        {
            WasAccessed = true;
            return Task.FromResult<MasterDataEntry?>(null);
        }

        public Task<bool> CodeOrEmailExistsAsync(
            MasterDataCategory category,
            string code,
            string? registrationNumber,
            string? email,
            Guid? exceptId,
            CancellationToken cancellationToken)
        {
            WasAccessed = true;
            return Task.FromResult(false);
        }

        public Task<MasterDataEntry> CreateAsync(
            MasterDataCategory category,
            MasterDataEntry entry,
            string? passwordHash,
            CancellationToken cancellationToken)
        {
            WasAccessed = true;
            return Task.FromResult(entry);
        }

        public Task<bool> UpdateAsync(
            MasterDataCategory category,
            MasterDataEntry entry,
            CancellationToken cancellationToken)
        {
            WasAccessed = true;
            return Task.FromResult(false);
        }

        public Task<bool> SetActiveAsync(
            MasterDataCategory category,
            Guid id,
            bool isActive,
            string actor,
            DateTime updatedAt,
            CancellationToken cancellationToken)
        {
            WasAccessed = true;
            return Task.FromResult(false);
        }
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password, int workFactor) => $"hashed:{password}";
    }
}
