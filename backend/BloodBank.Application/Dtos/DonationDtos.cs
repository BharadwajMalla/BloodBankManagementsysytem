namespace BloodBank.Application.Dtos;

public sealed class CompleteDonationCommand
{
    public bool DonorConfirmed { get; init; }
    public decimal? WeightKg { get; init; }
    public int? PulsePerMinute { get; init; }
    public decimal? Hemoglobin { get; init; }
    public decimal? Hematocrit { get; init; }
    public int? PlateletCount { get; init; }
    public string? BloodPressure { get; init; }
    public decimal? TemperatureCelsius { get; init; }
    public string? ScreeningStaffName { get; init; }
    public string? ScreeningStaffSignature { get; init; }
    public DateTime? ScreeningDate { get; init; }
    public string? ScreeningTime { get; init; }
    public bool MedicalExamAccepted { get; init; }
    public string? DeferralReason { get; init; }
    public string? MedicalOfficerName { get; init; }
    public string? MedicalOfficerSignature { get; init; }
    public DateTime? MedicalOfficerDate { get; init; }
    public string? DonorNumber { get; init; }
    public string? BagType { get; init; }
    public string? SegmentNumber { get; init; }
    public string? DonationStartTime { get; init; }
    public string? DonationEndTime { get; init; }
    public string? VenipunctureSite { get; init; }
    public string? PhlebotomistName { get; init; }
    public string? PhlebotomistSignature { get; init; }
    public bool VisualInspectionPassed { get; init; }
    public bool FeelsHealthyToday { get; init; }
    public bool DonatedRecently { get; init; }
    public bool HadDonationDiscomfort { get; init; }
    public bool AteRecently { get; init; }
    public bool SleptWell { get; init; }
    public bool HasInfectionRisk { get; init; }
    public bool HasColdFeverOrSinusitis { get; init; }
    public bool TookAntibioticsRecently { get; init; }
    public bool TookAlcoholRecently { get; init; }
    public bool SmokesFrequently { get; init; }
    public bool TookMedicationRecently { get; init; }
    public bool HadRecentVaccination { get; init; }
    public bool HadRecentIllness { get; init; }
    public bool HadRecentSurgeryOrTattoo { get; init; }
    public bool HadRecentMalariaOrEndemicExposure { get; init; }
    public bool HasChronicDisease { get; init; }
    public bool IsPregnantOrBreastfeeding { get; init; }
    public bool HadRecentAbortion { get; init; }
    public bool HasUnsafeSexualExposure { get; init; }
    public bool HasHivOrHepatitisRisk { get; init; }
    public bool HasPermanentDisqualification { get; init; }
    public int UnitsDonated { get; init; } = 1;
    public string? CampName { get; init; }
    public string? BagNumber { get; init; }
    public string? DonationCategory { get; init; }
    public string? ReplacementPatientName { get; init; }
    public string? ReplacementPatientHospital { get; init; }
    public string? ReplacementPatientIpNo { get; init; }
    public string? HbTestMethod { get; init; }
    public string? HbEligibilityStatus { get; init; }
    public string? DeferralRecords { get; init; }
    public string? CpdaBatchNumber { get; init; }
}

public sealed class DonationCompletionResultDto
{
    public bool IsEligible { get; init; }
    public string Message { get; init; } = string.Empty;
    public DonorDetailsDto? Donor { get; init; }
}

public sealed class DonationHistoryDto
{
    public Guid Id { get; init; }
    public Guid DonorId { get; init; }
    public string DonorName { get; init; } = string.Empty;
    public string BloodGroup { get; init; } = string.Empty;
    public int UnitsDonated { get; init; }
    public DateTime DonationDate { get; init; }
    public string? CampName { get; init; }
    public string BloodBankName { get; init; } = string.Empty;
    public string? BagNumber { get; init; }
    public string? DonationCategory { get; init; }
    public string? ReplacementPatientName { get; init; }
    public string? ReplacementPatientHospital { get; init; }
    public string? ReplacementPatientIpNo { get; init; }
    public string? HbTestMethod { get; init; }
    public string? HbEligibilityStatus { get; init; }
    public string? DeferralRecords { get; init; }
    public string? CpdaBatchNumber { get; init; }
    public decimal? WeightKg { get; init; }
    public int? PulsePerMinute { get; init; }
    public decimal? Hemoglobin { get; init; }
    public decimal? Hematocrit { get; init; }
    public int? PlateletCount { get; init; }
    public string? BloodPressure { get; init; }
    public decimal? TemperatureCelsius { get; init; }
    public string? ScreeningStaffName { get; init; }
    public DateTime? ScreeningDate { get; init; }
    public string? ScreeningTime { get; init; }
    public bool MedicalExamAccepted { get; init; }
    public string? MedicalOfficerName { get; init; }
    public DateTime? MedicalOfficerDate { get; init; }
    public string? DonorNumber { get; init; }
    public string? BagType { get; init; }
    public string? SegmentNumber { get; init; }
    public string? DonationStartTime { get; init; }
    public string? DonationEndTime { get; init; }
    public string? VenipunctureSite { get; init; }
    public string? PhlebotomistName { get; init; }
    public bool VisualInspectionPassed { get; init; }
    public bool DonorConfirmed { get; init; }
    public bool FeelsHealthyToday { get; init; }
    public bool DonatedRecently { get; init; }
    public bool HadDonationDiscomfort { get; init; }
    public bool AteRecently { get; init; }
    public bool SleptWell { get; init; }
    public bool HasInfectionRisk { get; init; }
    public bool HasColdFeverOrSinusitis { get; init; }
    public bool TookAntibioticsRecently { get; init; }
    public bool TookAlcoholRecently { get; init; }
    public bool SmokesFrequently { get; init; }
    public bool TookMedicationRecently { get; init; }
    public bool HadRecentVaccination { get; init; }
    public bool HadRecentIllness { get; init; }
    public bool HadRecentSurgeryOrTattoo { get; init; }
    public bool HadRecentMalariaOrEndemicExposure { get; init; }
    public bool HasChronicDisease { get; init; }
    public bool IsPregnantOrBreastfeeding { get; init; }
    public bool HadRecentAbortion { get; init; }
    public bool HasUnsafeSexualExposure { get; init; }
    public bool HasHivOrHepatitisRisk { get; init; }
    public bool HasPermanentDisqualification { get; init; }
}
