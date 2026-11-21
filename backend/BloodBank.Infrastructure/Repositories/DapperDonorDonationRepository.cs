using System.Data;
using Dapper;
using BloodBank.Application.Contracts;
using BloodBank.Application.Models;

namespace BloodBank.Infrastructure.Repositories;

public sealed class DapperDonorDonationRepository(
    Func<IDbConnection> createConnection,
    IDonorIdGenerator donorIdGenerator)
    : IDonorDonationRepository
{
    public async Task<IReadOnlyList<Donor>> GetDonorsAsync(CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        const string sql = @"
            SELECT Id, DonorId, FullName, Email, Phone, DateOfBirth, Sex, Address, BloodGroup,
                   CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
            FROM Users
            WHERE Role = 'Donor' AND IsActive = 1
            ORDER BY FullName ASC";

        var donors = await connection.QueryAsync<Donor>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return donors.AsList();
    }

    public async Task<Donor?> FindDonorAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        const string sql = @"
            SELECT Id, DonorId, FullName, Email, Phone, DateOfBirth, Sex, Address, BloodGroup,
                   CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
            FROM Users
            WHERE Id = @Id AND Role = 'Donor' AND IsActive = 1";

        return await connection.QuerySingleOrDefaultAsync<Donor>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UserEmailExistsAsync(
        string email,
        Guid? exceptUserId,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        const string sql = @"
            SELECT COUNT(1)
            FROM Users
            WHERE LOWER(Email) = LOWER(@Email)
              AND (@ExceptUserId IS NULL OR Id <> @ExceptUserId)";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { Email = email, ExceptUserId = exceptUserId },
                cancellationToken: cancellationToken)) > 0;
    }

    public async Task AddDonorAsync(
        Donor donor,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        donor.DonorId = await donorIdGenerator.GenerateAsync(cancellationToken);

        const string sql = @"
            INSERT INTO Users
                (Id, FullName, Email, PasswordHash, Role, Phone, DateOfBirth, Sex, Address,
                 BloodGroup, DonorId, HospitalName, HospitalRegNumber, IsEmailVerified, IsActive,
                 CreatedAt, CreatedBy)
            VALUES
                (@Id, @FullName, @Email, @PasswordHash, 'Donor', @Phone, @DateOfBirth, @Sex, @Address,
                 @BloodGroup, @DonorId, NULL, NULL, 0, 1, @CreatedAt, @CreatedBy)";

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    donor.Id,
                    donor.FullName,
                    donor.Email,
                    PasswordHash = passwordHash,
                    donor.Phone,
                    donor.DateOfBirth,
                    donor.Sex,
                    donor.Address,
                    donor.BloodGroup,
                    donor.DonorId,
                    donor.CreatedBy,
                    donor.CreatedAt
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateDonorAsync(Donor donor, CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        const string sql = @"
            UPDATE Users
            SET FullName = @FullName,
                Email = @Email,
                Phone = @Phone,
                DateOfBirth = @DateOfBirth,
                Sex = @Sex,
                Address = @Address,
                BloodGroup = @BloodGroup,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND Role = 'Donor'";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, donor, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeactivateDonorAsync(
        Guid id,
        string? actor,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        const string sql = @"
            UPDATE Users
            SET IsActive = 0,
            UpdatedAt = @UpdatedAt,
            UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND Role = 'Donor' AND IsActive = 1";

        return await connection.ExecuteAsync(
            new CommandDefinition(
            sql,
            new { Id = id, UpdatedAt = DateTime.UtcNow, UpdatedBy = actor },
            cancellationToken: cancellationToken)) > 0;
    }

    public async Task<IReadOnlyList<Donation>> GetDonationsAsync(
        IEnumerable<Guid> donorIds,
        CancellationToken cancellationToken)
    {
        var ids = donorIds.ToArray();
        if (ids.Length == 0)
            return Array.Empty<Donation>();

        using var connection = createConnection();
        const string sql = "SELECT * FROM DonationRecords WHERE DonorId IN @Ids";
        var donations = await connection.QueryAsync<Donation>(
            new CommandDefinition(sql, new { Ids = ids }, cancellationToken: cancellationToken));
        return donations.AsList();
    }

    public async Task<IReadOnlyList<Donation>> GetDonationsForDonorAsync(
        Guid donorId,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        const string sql = @"
            SELECT * FROM DonationRecords
            WHERE DonorId = @DonorId
            ORDER BY DonationDate DESC";

        var donations = await connection.QueryAsync<Donation>(
            new CommandDefinition(sql, new { DonorId = donorId }, cancellationToken: cancellationToken));
        return donations.AsList();
    }

    public async Task AddDonationAsync(Donation donation, CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var yearlyCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM DonationRecords WHERE DonationDate >= @Start AND DonationDate < @End",
            new { Start = new DateTime(donation.DonationDate.Year, 1, 1), End = new DateTime(donation.DonationDate.Year + 1, 1, 1) },
            transaction);
        var monthlyStart = new DateTime(donation.DonationDate.Year, donation.DonationDate.Month, 1);
        var monthlyCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM DonationRecords WHERE DonationDate >= @Start AND DonationDate < @End",
            new { Start = monthlyStart, End = monthlyStart.AddMonths(1) },
            transaction);
        donation.YearlySlNo = yearlyCount + 1;
        donation.MonthlySlNo = monthlyCount + 1;
        var bagNumber = donation.BagNumber?.Trim();
        if (string.IsNullOrWhiteSpace(bagNumber))
        {
            do
            {
                bagNumber = $"BAG-{donation.DonationDate:yyyyMMdd}-{Guid.NewGuid():N}";
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM BloodBags WHERE BagNumber = @BagNumber",
                    new { BagNumber = bagNumber }, transaction);
                if (count == 0) break;
            } while (true);
        }
        else
        {
            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM BloodBags WHERE BagNumber = @BagNumber",
                new { BagNumber = bagNumber }, transaction);
            if (exists > 0)
                throw new InvalidOperationException("Bag number already exists.");
        }
        donation.BagNumber = bagNumber;

        const string sql = @"
            INSERT INTO DonationRecords (
                Id, DonorId, DonorName, BloodGroup, UnitsDonated, DonationDate, CampName, BloodBankName,
                YearlySlNo, MonthlySlNo, DonationCategory, ReplacementPatientName, ReplacementPatientHospital,
                ReplacementPatientIpNo, BagNumber, CpdaBatchNumber, HbTestMethod, HbEligibilityStatus, DeferralRecords,
                WeightKg, PulsePerMinute, Hemoglobin, Hematocrit, PlateletCount, BloodPressure, TemperatureCelsius,
                ScreeningStaffName, ScreeningStaffSignature, ScreeningDate, ScreeningTime, MedicalExamAccepted,
                DeferralReason, MedicalOfficerName, MedicalOfficerSignature, MedicalOfficerDate, DonorNumber,
                BagType, SegmentNumber, DonationStartTime, DonationEndTime, VenipunctureSite, PhlebotomistName,
                PhlebotomistSignature, VisualInspectionPassed, DonorConfirmed, FeelsHealthyToday, DonatedRecently,
                HadDonationDiscomfort, AteRecently, SleptWell, HasInfectionRisk, HasColdFeverOrSinusitis,
                TookAntibioticsRecently, TookAlcoholRecently, SmokesFrequently, TookMedicationRecently,
                HadRecentVaccination, HadRecentIllness, HadRecentSurgeryOrTattoo, HadRecentMalariaOrEndemicExposure,
                HasChronicDisease, IsPregnantOrBreastfeeding, HadRecentAbortion, HasUnsafeSexualExposure,
                HasHivOrHepatitisRisk, HasPermanentDisqualification
            ) VALUES (
                @Id, @DonorId, @DonorName, @BloodGroup, @UnitsDonated, @DonationDate, @CampName, @BloodBankName,
                @YearlySlNo, @MonthlySlNo, @DonationCategory, @ReplacementPatientName, @ReplacementPatientHospital,
                @ReplacementPatientIpNo, @BagNumber, @CpdaBatchNumber, @HbTestMethod, @HbEligibilityStatus, @DeferralRecords,
                @WeightKg, @PulsePerMinute, @Hemoglobin, @Hematocrit, @PlateletCount, @BloodPressure, @TemperatureCelsius,
                @ScreeningStaffName, @ScreeningStaffSignature, @ScreeningDate, @ScreeningTime, @MedicalExamAccepted,
                @DeferralReason, @MedicalOfficerName, @MedicalOfficerSignature, @MedicalOfficerDate, @DonorNumber,
                @BagType, @SegmentNumber, @DonationStartTime, @DonationEndTime, @VenipunctureSite, @PhlebotomistName,
                @PhlebotomistSignature, @VisualInspectionPassed, @DonorConfirmed, @FeelsHealthyToday, @DonatedRecently,
                @HadDonationDiscomfort, @AteRecently, @SleptWell, @HasInfectionRisk, @HasColdFeverOrSinusitis,
                @TookAntibioticsRecently, @TookAlcoholRecently, @SmokesFrequently, @TookMedicationRecently,
                @HadRecentVaccination, @HadRecentIllness, @HadRecentSurgeryOrTattoo, @HadRecentMalariaOrEndemicExposure,
                @HasChronicDisease, @IsPregnantOrBreastfeeding, @HadRecentAbortion, @HasUnsafeSexualExposure,
                @HasHivOrHepatitisRisk, @HasPermanentDisqualification
            )";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, donation, transaction: transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(
            new CommandDefinition(@"
                INSERT INTO BloodBags (
                    Id, BagNumber, BloodGroup, ComponentType, QuantityUnits, AddedDate, IsIssued, IssuedDate,
                    Status, ExpiryDate, ScreeningStatus, ReservedUntil, IssuedBy, IssuedAt, CpdaBatchNumber
                ) VALUES (
                    @Id, @BagNumber, @BloodGroup, 'Whole Blood', @UnitsDonated, @DonationDate, 0, NULL,
                    'Available', @ExpiryDate, 'Unscreened', NULL, NULL, NULL, @CpdaBatchNumber
                )",
                new
                {
                    Id = Guid.NewGuid(),
                    BagNumber = donation.BagNumber,
                    donation.BloodGroup,
                    UnitsDonated = 1,
                    donation.DonationDate,
                    ExpiryDate = donation.DonationDate.AddDays(35),
                    donation.CpdaBatchNumber
                },
                transaction,
                cancellationToken: cancellationToken));
        transaction.Commit();
    }
}
