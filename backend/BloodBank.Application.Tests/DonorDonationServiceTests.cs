using BloodBank.Application.Contracts;
using BloodBank.Application.Dtos;
using BloodBank.Application.Models;
using BloodBank.Application.Services;
using Xunit;

namespace BloodBank.Application.Tests;

public sealed class DonorDonationServiceTests
{
    [Fact]
    public async Task CompleteDonationRejectsDonorWithRecentDonation()
    {
        var donor = CreateDonor();
        var repository = new FakeDonorDonationRepository(donor);
        repository.Donations.Add(new Donation
        {
            DonorId = donor.Id,
            DonationDate = DateTime.UtcNow.AddDays(-30)
        });
        var service = CreateService(repository);

        var result = await service.CompleteDonationAsync(donor.Id, EligibleCommand(), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Equal("Donor must wait 90 days between donations.", result.Message);
        Assert.Empty(repository.AddedDonations);
    }

    [Fact]
    public async Task CompleteDonationRejectsPermanentDeferralAndDoesNotPersist()
    {
        var donor = CreateDonor();
        var repository = new FakeDonorDonationRepository(donor);
        var service = CreateService(repository);
        var command = EligibleCommand(permanentDeferral: true);

        var result = await service.CompleteDonationAsync(donor.Id, command, CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Equal("Donor has a permanent disqualification.", result.Message);
        Assert.Empty(repository.AddedDonations);
    }

    [Fact]
    public async Task CompleteDonationRejectsFemaleDonorWithNinetyDayInterval()
    {
        var donor = CreateDonor();
        donor.Sex = "Female";
        var repository = new FakeDonorDonationRepository(donor);
        repository.Donations.Add(new Donation { DonorId = donor.Id, DonationDate = DateTime.UtcNow.AddDays(-100) });
        var service = CreateService(repository);

        var result = await service.CompleteDonationAsync(donor.Id, EligibleCommand(hemoglobin: 13m, weightKg: 50m), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Equal("Donor must wait 120 days between donations.", result.Message);
    }

    [Fact]
    public async Task CompleteDonationRejectsLowWeightAndHemoglobin()
    {
        var repository = new FakeDonorDonationRepository(CreateDonor());
        var service = CreateService(repository);

        var weightResult = await service.CompleteDonationAsync(repository.DonorId, EligibleCommand(hemoglobin: 13m, weightKg: 44m), CancellationToken.None);
        var hbResult = await service.CompleteDonationAsync(repository.DonorId, EligibleCommand(hemoglobin: 12m, weightKg: 50m), CancellationToken.None);

        Assert.False(weightResult.IsEligible);
        Assert.Equal("Donor weight must be at least 45 kg.", weightResult.Message);
        Assert.False(hbResult.IsEligible);
        Assert.Equal("Donor hemoglobin must be at least 12.5 g/dL.", hbResult.Message);
    }

    [Fact]
    public async Task CompleteDonationPersistsSuccessfulDonationAndClampsUnits()
    {
        var donor = CreateDonor();
        var repository = new FakeDonorDonationRepository(donor);
        var service = CreateService(repository);
        var command = EligibleCommand(unitsDonated: 11, hemoglobin: 13.2m);

        var result = await service.CompleteDonationAsync(donor.Id, command, CancellationToken.None);

        Assert.True(result.IsEligible);
        Assert.Equal("Donation completed successfully.", result.Message);
        var saved = Assert.Single(repository.AddedDonations);
        Assert.Equal(10, saved.UnitsDonated);
        Assert.Equal(13.2m, saved.Hemoglobin);
        Assert.Equal(donor.Id, saved.DonorId);
    }

    [Fact]
    public async Task CreateDonorNormalizesEmailAndReturnsGeneratedPassword()
    {
        var repository = new FakeDonorDonationRepository();
        var hasher = new FakePasswordHasher();
        var service = new DonorDonationService(repository, hasher);
        var command = new CreateDonorCommand(
            "Jane",
            "Smith",
            " Jane.Smith@Example.com ",
            "5550100",
            DateTime.UtcNow.Date.AddYears(-30),
            "Female",
            "Address",
            "o+");

        var result = await service.CreateDonorAsync(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("jane.smith@example.com", result.Donor.Email);
        Assert.Equal("SMITH@" + command.DateOfBirth.Year, result.GeneratedPassword);
        Assert.Equal("hashed:" + result.GeneratedPassword, repository.CreatedPasswordHash);
        Assert.Equal("O+", result.Donor.BloodGroup);
    }

    [Fact]
    public async Task DeactivateDonorDelegatesToRepositoryWithoutDeletingHistory()
    {
        var donor = CreateDonor();
        var repository = new FakeDonorDonationRepository(donor);
        repository.Donations.Add(new Donation { DonorId = donor.Id });
        var service = CreateService(repository);

        var deactivated = await service.DeactivateDonorAsync(donor.Id, CancellationToken.None, "staff");

        Assert.True(deactivated);
        Assert.Equal(donor.Id, Assert.Single(repository.Donations).DonorId);
        Assert.Equal("staff", repository.DeactivationActor);
    }

    private static DonorDonationService CreateService(FakeDonorDonationRepository repository) =>
        new(repository, new FakePasswordHasher());

    private static Donor CreateDonor() =>
        new()
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Smith",
            Email = "jane@example.com",
            DateOfBirth = DateTime.UtcNow.Date.AddYears(-30),
            BloodGroup = "O+",
            CreatedAt = DateTime.UtcNow
        };

    private static CompleteDonationCommand EligibleCommand(
        int unitsDonated = 1,
        decimal? hemoglobin = null,
        decimal? weightKg = 50m,
        bool permanentDeferral = false) =>
        new()
        {
            DonorConfirmed = true,
            MedicalExamAccepted = true,
            FeelsHealthyToday = true,
            VisualInspectionPassed = true,
            AteRecently = true,
            SleptWell = true,
            UnitsDonated = unitsDonated,
            Hemoglobin = hemoglobin,
            WeightKg = weightKg,
            HasPermanentDisqualification = permanentDeferral
        };

    private sealed class FakeDonorDonationRepository : IDonorDonationRepository
    {
        private Donor? _donor;

        public FakeDonorDonationRepository(Donor? donor = null)
        {
            _donor = donor;
        }

        public Guid DonorId => _donor?.Id ?? Guid.Empty;

        public List<Donation> Donations { get; } = [];
        public List<Donation> AddedDonations { get; } = [];
        public string? CreatedPasswordHash { get; private set; }
        public string? DeactivationActor { get; private set; }

        public Task<IReadOnlyList<Donor>> GetDonorsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Donor>>(_donor is null ? [] : [_donor]);

        public Task<Donor?> FindDonorAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_donor?.Id == id ? _donor : null);

        public Task<bool> UserEmailExistsAsync(
            string email,
            Guid? exceptUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_donor is not null
                && _donor.Id != exceptUserId
                && _donor.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        public Task AddDonorAsync(Donor newDonor, string passwordHash, CancellationToken cancellationToken)
        {
            _donor = newDonor;
            CreatedPasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateDonorAsync(Donor updatedDonor, CancellationToken cancellationToken)
        {
            _donor = updatedDonor;
            return Task.FromResult(true);
        }

        public Task<bool> DeactivateDonorAsync(Guid id, string? actor, CancellationToken cancellationToken)
        {
            DeactivationActor = actor;
            var found = _donor?.Id == id;
            if (found) _donor = null;
            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<Donation>> GetDonationsAsync(
            IEnumerable<Guid> donorIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Donation>>(Donations.Where(item => donorIds.Contains(item.DonorId)).ToList());

        public Task<IReadOnlyList<Donation>> GetDonationsForDonorAsync(
            Guid donorId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Donation>>(Donations.Where(item => item.DonorId == donorId).ToList());

        public Task AddDonationAsync(Donation donation, CancellationToken cancellationToken)
        {
            AddedDonations.Add(donation);
            Donations.Add(donation);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password, int workFactor) => $"hashed:{password}";
    }
}
