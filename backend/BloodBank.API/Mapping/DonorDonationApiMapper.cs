using BloodBank.API.DTOs;
using AppDonationCompletionResultDto = BloodBank.Application.Dtos.DonationCompletionResultDto;
using AppDonationHistoryDto = BloodBank.Application.Dtos.DonationHistoryDto;
using AppDonorDetailsDto = BloodBank.Application.Dtos.DonorDetailsDto;
using AppCreateDonorResponseDto = BloodBank.Application.Dtos.CreateDonorResponseDto;
using AppCompleteDonationCommand = BloodBank.Application.Dtos.CompleteDonationCommand;
using AppCreateDonorCommand = BloodBank.Application.Dtos.CreateDonorCommand;

namespace BloodBank.API.Mapping;

internal static class DonorDonationApiMapper
{
    public static AppCreateDonorCommand ToCommand(CreateDonorDto dto) =>
        new(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.Phone,
            dto.DateOfBirth!.Value,
            dto.Sex,
            dto.Address,
            dto.BloodGroup);

    public static AppCompleteDonationCommand ToCommand(CompleteDonationDto dto) =>
        new()
        {
            DonorConfirmed = dto.DonorConfirmed,
            WeightKg = dto.WeightKg,
            PulsePerMinute = dto.PulsePerMinute,
            Hemoglobin = dto.Hemoglobin,
            Hematocrit = dto.Hematocrit,
            PlateletCount = dto.PlateletCount,
            BloodPressure = dto.BloodPressure,
            TemperatureCelsius = dto.TemperatureCelsius,
            ScreeningStaffName = dto.ScreeningStaffName,
            ScreeningStaffSignature = dto.ScreeningStaffSignature,
            ScreeningDate = dto.ScreeningDate,
            ScreeningTime = dto.ScreeningTime,
            MedicalExamAccepted = dto.MedicalExamAccepted,
            DeferralReason = dto.DeferralReason,
            MedicalOfficerName = dto.MedicalOfficerName,
            MedicalOfficerSignature = dto.MedicalOfficerSignature,
            MedicalOfficerDate = dto.MedicalOfficerDate,
            DonorNumber = dto.DonorNumber,
            BagType = dto.BagType,
            SegmentNumber = dto.SegmentNumber,
            DonationStartTime = dto.DonationStartTime,
            DonationEndTime = dto.DonationEndTime,
            VenipunctureSite = dto.VenipunctureSite,
            PhlebotomistName = dto.PhlebotomistName,
            PhlebotomistSignature = dto.PhlebotomistSignature,
            VisualInspectionPassed = dto.VisualInspectionPassed,
            FeelsHealthyToday = dto.FeelsHealthyToday,
            DonatedRecently = dto.DonatedRecently,
            HadDonationDiscomfort = dto.HadDonationDiscomfort,
            AteRecently = dto.AteRecently,
            SleptWell = dto.SleptWell,
            HasInfectionRisk = dto.HasInfectionRisk,
            HasColdFeverOrSinusitis = dto.HasColdFeverOrSinusitis,
            TookAntibioticsRecently = dto.TookAntibioticsRecently,
            TookAlcoholRecently = dto.TookAlcoholRecently,
            SmokesFrequently = dto.SmokesFrequently,
            TookMedicationRecently = dto.TookMedicationRecently,
            HadRecentVaccination = dto.HadRecentVaccination,
            HadRecentIllness = dto.HadRecentIllness,
            HadRecentSurgeryOrTattoo = dto.HadRecentSurgeryOrTattoo,
            HadRecentMalariaOrEndemicExposure = dto.HadRecentMalariaOrEndemicExposure,
            HasChronicDisease = dto.HasChronicDisease,
            IsPregnantOrBreastfeeding = dto.IsPregnantOrBreastfeeding,
            HadRecentAbortion = dto.HadRecentAbortion,
            HasUnsafeSexualExposure = dto.HasUnsafeSexualExposure,
            HasHivOrHepatitisRisk = dto.HasHivOrHepatitisRisk,
            HasPermanentDisqualification = dto.HasPermanentDisqualification,
            UnitsDonated = dto.UnitsDonated,
            CampName = dto.CampName,
            BagNumber = dto.BagNumber,
            DonationCategory = dto.DonationCategory,
            ReplacementPatientName = dto.ReplacementPatientName,
            ReplacementPatientHospital = dto.ReplacementPatientHospital,
            ReplacementPatientIpNo = dto.ReplacementPatientIpNo,
            HbTestMethod = dto.HbTestMethod,
            HbEligibilityStatus = dto.HbEligibilityStatus,
            DeferralRecords = dto.DeferralRecords,
            CpdaBatchNumber = dto.CpdaBatchNumber
        };

    public static DonorDetailsDto ToApi(AppDonorDetailsDto dto) =>
        new()
        {
            Id = dto.Id,
            DonorId = dto.DonorId,
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            DateOfBirth = dto.DateOfBirth,
            Sex = dto.Sex,
            Address = dto.Address,
            BloodGroup = dto.BloodGroup,
            RegisteredDate = dto.RegisteredDate,
            TotalDonations = dto.TotalDonations,
            LastDonationDate = dto.LastDonationDate
        };

    public static CreateDonorResponseDto ToApi(AppCreateDonorResponseDto dto) =>
        new()
        {
            Donor = ToApi(dto.Donor),
            GeneratedPassword = dto.GeneratedPassword
        };

    public static DonationCompletionResultDto ToApi(AppDonationCompletionResultDto dto) =>
        new()
        {
            IsEligible = dto.IsEligible,
            Message = dto.Message,
            Donor = dto.Donor is null ? null : ToApi(dto.Donor)
        };

    public static DonationHistoryDto ToApi(AppDonationHistoryDto dto) =>
        new()
        {
            Id = dto.Id,
            DonorId = dto.DonorId,
            DonorName = dto.DonorName,
            BloodGroup = dto.BloodGroup,
            UnitsDonated = dto.UnitsDonated,
            DonationDate = dto.DonationDate,
            CampName = dto.CampName,
            BloodBankName = dto.BloodBankName,
            WeightKg = dto.WeightKg,
            PulsePerMinute = dto.PulsePerMinute,
            Hemoglobin = dto.Hemoglobin,
            Hematocrit = dto.Hematocrit,
            PlateletCount = dto.PlateletCount,
            BloodPressure = dto.BloodPressure,
            TemperatureCelsius = dto.TemperatureCelsius,
            ScreeningStaffName = dto.ScreeningStaffName,
            ScreeningDate = dto.ScreeningDate,
            ScreeningTime = dto.ScreeningTime,
            MedicalExamAccepted = dto.MedicalExamAccepted,
            MedicalOfficerName = dto.MedicalOfficerName,
            MedicalOfficerDate = dto.MedicalOfficerDate,
            DonorNumber = dto.DonorNumber,
            BagType = dto.BagType,
            SegmentNumber = dto.SegmentNumber,
            DonationStartTime = dto.DonationStartTime,
            DonationEndTime = dto.DonationEndTime,
            VenipunctureSite = dto.VenipunctureSite,
            PhlebotomistName = dto.PhlebotomistName,
            VisualInspectionPassed = dto.VisualInspectionPassed,
            DonorConfirmed = dto.DonorConfirmed,
            FeelsHealthyToday = dto.FeelsHealthyToday,
            DonatedRecently = dto.DonatedRecently,
            HadDonationDiscomfort = dto.HadDonationDiscomfort,
            AteRecently = dto.AteRecently,
            SleptWell = dto.SleptWell,
            HasInfectionRisk = dto.HasInfectionRisk,
            HasColdFeverOrSinusitis = dto.HasColdFeverOrSinusitis,
            TookAntibioticsRecently = dto.TookAntibioticsRecently,
            TookAlcoholRecently = dto.TookAlcoholRecently,
            SmokesFrequently = dto.SmokesFrequently,
            TookMedicationRecently = dto.TookMedicationRecently,
            HadRecentVaccination = dto.HadRecentVaccination,
            HadRecentIllness = dto.HadRecentIllness,
            HadRecentSurgeryOrTattoo = dto.HadRecentSurgeryOrTattoo,
            HadRecentMalariaOrEndemicExposure = dto.HadRecentMalariaOrEndemicExposure,
            HasChronicDisease = dto.HasChronicDisease,
            IsPregnantOrBreastfeeding = dto.IsPregnantOrBreastfeeding,
            HadRecentAbortion = dto.HadRecentAbortion,
            HasUnsafeSexualExposure = dto.HasUnsafeSexualExposure,
            HasHivOrHepatitisRisk = dto.HasHivOrHepatitisRisk,
            HasPermanentDisqualification = dto.HasPermanentDisqualification
        };
}
