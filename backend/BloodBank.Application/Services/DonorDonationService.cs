using BloodBank.Application.Contracts;
using BloodBank.Application.Dtos;
using BloodBank.Application.Models;

namespace BloodBank.Application.Services;

public sealed class DonorDonationService(
    IDonorDonationRepository repository,
    IPasswordHasher passwordHasher) : IDonorDonationService
{
    public async Task<IReadOnlyList<DonorDetailsDto>> GetDonorsAsync(CancellationToken cancellationToken)
    {
        var donors = await repository.GetDonorsAsync(cancellationToken);
        var donations = await repository.GetDonationsAsync(
            donors.Select(donor => donor.Id),
            cancellationToken);

        return donors.Select(donor =>
        {
            var donorDonations = donations
                .Where(donation => donation.DonorId == donor.Id)
                .OrderByDescending(donation => donation.DonationDate)
                .ToList();

            return MapDonor(donor, donorDonations);
        }).ToList();
    }

    public async Task<CreateDonorResponseDto?> CreateDonorAsync(
        CreateDonorCommand command,
        CancellationToken cancellationToken,
        string? actor = null)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        if (await repository.UserEmailExistsAsync(email, null, cancellationToken))
            return null;

        var fullName = $"{command.FirstName.Trim()} {command.LastName.Trim()}";
        var generatedPassword = $"{command.LastName.Trim().ToUpperInvariant()}@{command.DateOfBirth.Year}";
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Phone = command.Phone.Trim(),
            DateOfBirth = command.DateOfBirth,
            Sex = command.Sex.Trim(),
            Address = command.Address.Trim(),
            BloodGroup = command.BloodGroup.Trim().ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actor ?? email
        };

        var passwordHash = passwordHasher.HashPassword(generatedPassword, 11);
        await repository.AddDonorAsync(donor, passwordHash, cancellationToken);

        return new CreateDonorResponseDto
        {
            Donor = MapDonor(donor, Array.Empty<Donation>()),
            GeneratedPassword = generatedPassword
        };
    }

    public async Task<DonorDetailsDto?> UpdateDonorAsync(
        Guid id,
        CreateDonorCommand command,
        CancellationToken cancellationToken,
        string? actor = null)
    {
        var donor = await repository.FindDonorAsync(id, cancellationToken);
        if (donor is null)
            return null;

        var email = command.Email.Trim().ToLowerInvariant();
        if (await repository.UserEmailExistsAsync(email, id, cancellationToken))
            return null;

        donor.FullName = $"{command.FirstName.Trim()} {command.LastName.Trim()}";
        donor.Email = email;
        donor.Phone = command.Phone.Trim();
        donor.DateOfBirth = command.DateOfBirth;
        donor.Sex = command.Sex.Trim();
        donor.Address = command.Address.Trim();
        donor.BloodGroup = command.BloodGroup.Trim().ToUpperInvariant();
        donor.UpdatedAt = DateTime.UtcNow;
        donor.UpdatedBy = actor ?? email;

        if (!await repository.UpdateDonorAsync(donor, cancellationToken))
            return null;

        var donations = await repository.GetDonationsForDonorAsync(id, cancellationToken);
        return MapDonor(donor, donations);
    }

    public Task<bool> DeactivateDonorAsync(
        Guid id,
        CancellationToken cancellationToken,
        string? actor = null) =>
        repository.DeactivateDonorAsync(id, actor, cancellationToken);

    public async Task<DonationCompletionResultDto> CompleteDonationAsync(
        Guid donorId,
        CompleteDonationCommand command,
        CancellationToken cancellationToken)
    {
        var donor = await repository.FindDonorAsync(donorId, cancellationToken);
        if (donor is null)
            return Ineligible("Donor not found.");

        var donations = await repository.GetDonationsForDonorAsync(donorId, cancellationToken);
        var lastDonation = donations.OrderByDescending(item => item.DonationDate).FirstOrDefault();
        var age = GetAge(donor.DateOfBirth);

        if (age < 18 || age > 60)
            return Ineligible("Donor must be between 18 and 60 years old.");
        var intervalDays = donor.Sex?.Trim().StartsWith("Female", StringComparison.OrdinalIgnoreCase) == true ? 120 : 90;
        if (lastDonation is not null && (DateTime.UtcNow - lastDonation.DonationDate).TotalDays < intervalDays)
            return Ineligible($"Donor must wait {intervalDays} days between donations.");
        if (!command.FeelsHealthyToday)
            return Ineligible("Donor must confirm they feel healthy today.");
        if (HasTemporaryDeferral(command))
            return Ineligible("Donor has a temporary deferral and cannot donate yet.");
        if (command.HasPermanentDisqualification)
            return Ineligible("Donor has a permanent disqualification.");
        if (!command.DonorConfirmed)
            return Ineligible("Donor confirmation is required before completing the donation.");
        if (!command.MedicalExamAccepted)
            return Ineligible("Medical examination must be accepted by blood-bank staff.");
        if (!command.WeightKg.HasValue || command.WeightKg.Value < 45m)
            return Ineligible("Donor weight must be at least 45 kg.");
        if (!command.Hemoglobin.HasValue || command.Hemoglobin.Value < 12.5m)
            return Ineligible("Donor hemoglobin must be at least 12.5 g/dL.");

        await repository.AddDonationAsync(MapDonation(donor, command), cancellationToken);

        var updatedDonor = (await GetDonorsAsync(cancellationToken))
            .First(item => item.Id == donor.Id);
        return new DonationCompletionResultDto
        {
            IsEligible = true,
            Message = "Donation completed successfully.",
            Donor = updatedDonor
        };
    }

    public async Task<IReadOnlyList<DonationHistoryDto>> GetDonationHistoryAsync(
        Guid donorId,
        CancellationToken cancellationToken)
    {
        var donations = await repository.GetDonationsForDonorAsync(donorId, cancellationToken);
        return donations.Select(MapHistory).ToList();
    }

    private static DonationCompletionResultDto Ineligible(string message) =>
        new() { Message = message };

    private static bool HasTemporaryDeferral(CompleteDonationCommand command) =>
        command.DonatedRecently || command.HadDonationDiscomfort || !command.AteRecently || !command.SleptWell
        || command.HasInfectionRisk || command.HasColdFeverOrSinusitis || command.TookAntibioticsRecently
        || command.TookAlcoholRecently || command.SmokesFrequently || command.TookMedicationRecently
        || command.HadRecentVaccination || command.HadRecentIllness || command.HadRecentSurgeryOrTattoo
        || command.HadRecentMalariaOrEndemicExposure || command.HasChronicDisease
        || command.IsPregnantOrBreastfeeding || command.HadRecentAbortion
        || command.HasUnsafeSexualExposure || command.HasHivOrHepatitisRisk;

    private static int GetAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
            return 0;

        var today = DateTime.UtcNow.Date;
        var age = today.Year - dateOfBirth.Value.Year;
        return today < dateOfBirth.Value.Date.AddYears(age) ? age - 1 : age;
    }

    private static DonorDetailsDto MapDonor(Donor donor, IReadOnlyCollection<Donation> donations)
    {
        var lastDonation = donations
            .OrderByDescending(donation => donation.DonationDate)
            .FirstOrDefault();

        return new DonorDetailsDto
        {
            Id = donor.Id,
            DonorId = donor.DonorId ?? string.Empty,
            FullName = donor.FullName,
            Email = donor.Email,
            Phone = donor.Phone,
            DateOfBirth = donor.DateOfBirth,
            Sex = donor.Sex,
            Address = donor.Address,
            BloodGroup = donor.BloodGroup,
            RegisteredDate = donor.CreatedAt,
            UpdatedAt = donor.UpdatedAt,
            CreatedBy = donor.CreatedBy,
            UpdatedBy = donor.UpdatedBy,
            TotalDonations = donations.Count,
            LastDonationDate = lastDonation?.DonationDate
        };
    }

    private static Donation MapDonation(Donor donor, CompleteDonationCommand command) =>
        new()
        {
            DonorId = donor.Id,
            DonorName = donor.FullName,
            BloodGroup = donor.BloodGroup ?? string.Empty,
            UnitsDonated = Math.Clamp(command.UnitsDonated, 1, 10),
            DonationDate = DateTime.UtcNow,
            CampName = command.CampName?.Trim(),
            BagNumber = command.BagNumber?.Trim(),
            DonationCategory = string.IsNullOrWhiteSpace(command.DonationCategory) ? "Voluntary" : command.DonationCategory.Trim(),
            ReplacementPatientName = command.ReplacementPatientName?.Trim(),
            ReplacementPatientHospital = command.ReplacementPatientHospital?.Trim(),
            ReplacementPatientIpNo = command.ReplacementPatientIpNo?.Trim(),
            HbTestMethod = command.HbTestMethod?.Trim(),
            HbEligibilityStatus = command.HbEligibilityStatus?.Trim(),
            DeferralRecords = command.DeferralRecords?.Trim(),
            CpdaBatchNumber = command.CpdaBatchNumber?.Trim(),
            WeightKg = command.WeightKg,
            PulsePerMinute = command.PulsePerMinute,
            Hemoglobin = command.Hemoglobin,
            Hematocrit = command.Hematocrit,
            PlateletCount = command.PlateletCount,
            BloodPressure = command.BloodPressure,
            TemperatureCelsius = command.TemperatureCelsius,
            ScreeningStaffName = command.ScreeningStaffName,
            ScreeningStaffSignature = command.ScreeningStaffSignature,
            ScreeningDate = command.ScreeningDate,
            ScreeningTime = command.ScreeningTime,
            MedicalExamAccepted = command.MedicalExamAccepted,
            DeferralReason = command.DeferralReason,
            MedicalOfficerName = command.MedicalOfficerName,
            MedicalOfficerSignature = command.MedicalOfficerSignature,
            MedicalOfficerDate = command.MedicalOfficerDate,
            DonorNumber = command.DonorNumber,
            BagType = command.BagType,
            SegmentNumber = command.SegmentNumber,
            DonationStartTime = command.DonationStartTime,
            DonationEndTime = command.DonationEndTime,
            VenipunctureSite = command.VenipunctureSite,
            PhlebotomistName = command.PhlebotomistName,
            PhlebotomistSignature = command.PhlebotomistSignature,
            VisualInspectionPassed = command.VisualInspectionPassed,
            DonorConfirmed = command.DonorConfirmed,
            FeelsHealthyToday = command.FeelsHealthyToday,
            DonatedRecently = command.DonatedRecently,
            HadDonationDiscomfort = command.HadDonationDiscomfort,
            AteRecently = command.AteRecently,
            SleptWell = command.SleptWell,
            HasInfectionRisk = command.HasInfectionRisk,
            HasColdFeverOrSinusitis = command.HasColdFeverOrSinusitis,
            TookAntibioticsRecently = command.TookAntibioticsRecently,
            TookAlcoholRecently = command.TookAlcoholRecently,
            SmokesFrequently = command.SmokesFrequently,
            TookMedicationRecently = command.TookMedicationRecently,
            HadRecentVaccination = command.HadRecentVaccination,
            HadRecentIllness = command.HadRecentIllness,
            HadRecentSurgeryOrTattoo = command.HadRecentSurgeryOrTattoo,
            HadRecentMalariaOrEndemicExposure = command.HadRecentMalariaOrEndemicExposure,
            HasChronicDisease = command.HasChronicDisease,
            IsPregnantOrBreastfeeding = command.IsPregnantOrBreastfeeding,
            HadRecentAbortion = command.HadRecentAbortion,
            HasUnsafeSexualExposure = command.HasUnsafeSexualExposure,
            HasHivOrHepatitisRisk = command.HasHivOrHepatitisRisk,
            HasPermanentDisqualification = command.HasPermanentDisqualification
        };

    private static DonationHistoryDto MapHistory(Donation donation) =>
        new()
        {
            Id = donation.Id,
            DonorId = donation.DonorId,
            DonorName = donation.DonorName,
            BloodGroup = donation.BloodGroup,
            UnitsDonated = donation.UnitsDonated,
            DonationDate = donation.DonationDate,
            CampName = donation.CampName,
            BloodBankName = donation.BloodBankName,
            BagNumber = donation.BagNumber,
            DonationCategory = donation.DonationCategory,
            ReplacementPatientName = donation.ReplacementPatientName,
            ReplacementPatientHospital = donation.ReplacementPatientHospital,
            ReplacementPatientIpNo = donation.ReplacementPatientIpNo,
            HbTestMethod = donation.HbTestMethod,
            HbEligibilityStatus = donation.HbEligibilityStatus,
            DeferralRecords = donation.DeferralRecords,
            CpdaBatchNumber = donation.CpdaBatchNumber,
            WeightKg = donation.WeightKg,
            PulsePerMinute = donation.PulsePerMinute,
            Hemoglobin = donation.Hemoglobin,
            Hematocrit = donation.Hematocrit,
            PlateletCount = donation.PlateletCount,
            BloodPressure = donation.BloodPressure,
            TemperatureCelsius = donation.TemperatureCelsius,
            ScreeningStaffName = donation.ScreeningStaffName,
            ScreeningDate = donation.ScreeningDate,
            ScreeningTime = donation.ScreeningTime,
            MedicalExamAccepted = donation.MedicalExamAccepted,
            MedicalOfficerName = donation.MedicalOfficerName,
            MedicalOfficerDate = donation.MedicalOfficerDate,
            DonorNumber = donation.DonorNumber,
            BagType = donation.BagType,
            SegmentNumber = donation.SegmentNumber,
            DonationStartTime = donation.DonationStartTime,
            DonationEndTime = donation.DonationEndTime,
            VenipunctureSite = donation.VenipunctureSite,
            PhlebotomistName = donation.PhlebotomistName,
            VisualInspectionPassed = donation.VisualInspectionPassed,
            DonorConfirmed = donation.DonorConfirmed,
            FeelsHealthyToday = donation.FeelsHealthyToday,
            DonatedRecently = donation.DonatedRecently,
            HadDonationDiscomfort = donation.HadDonationDiscomfort,
            AteRecently = donation.AteRecently,
            SleptWell = donation.SleptWell,
            HasInfectionRisk = donation.HasInfectionRisk,
            HasColdFeverOrSinusitis = donation.HasColdFeverOrSinusitis,
            TookAntibioticsRecently = donation.TookAntibioticsRecently,
            TookAlcoholRecently = donation.TookAlcoholRecently,
            SmokesFrequently = donation.SmokesFrequently,
            TookMedicationRecently = donation.TookMedicationRecently,
            HadRecentVaccination = donation.HadRecentVaccination,
            HadRecentIllness = donation.HadRecentIllness,
            HadRecentSurgeryOrTattoo = donation.HadRecentSurgeryOrTattoo,
            HadRecentMalariaOrEndemicExposure = donation.HadRecentMalariaOrEndemicExposure,
            HasChronicDisease = donation.HasChronicDisease,
            IsPregnantOrBreastfeeding = donation.IsPregnantOrBreastfeeding,
            HadRecentAbortion = donation.HadRecentAbortion,
            HasUnsafeSexualExposure = donation.HasUnsafeSexualExposure,
            HasHivOrHepatitisRisk = donation.HasHivOrHepatitisRisk,
            HasPermanentDisqualification = donation.HasPermanentDisqualification
        };
}
