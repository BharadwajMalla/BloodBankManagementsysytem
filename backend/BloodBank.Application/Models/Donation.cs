namespace BloodBank.Application.Models;

public sealed class Donation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DonorId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public int UnitsDonated { get; set; } = 1;
    public DateTime DonationDate { get; set; } = DateTime.UtcNow;
    public string? CampName { get; set; }
    public string BloodBankName { get; set; } = "Blood Bank A";
    public int? YearlySlNo { get; set; }
    public int? MonthlySlNo { get; set; }
    public string DonationCategory { get; set; } = "Voluntary";
    public string? ReplacementPatientName { get; set; }
    public string? ReplacementPatientHospital { get; set; }
    public string? ReplacementPatientIpNo { get; set; }
    public string? BagNumber { get; set; }
    public string? HbTestMethod { get; set; } = "Hemocue";
    public string? HbEligibilityStatus { get; set; } = "Fit";
    public string? DeferralRecords { get; set; }
    public string? CpdaBatchNumber { get; set; }
    public decimal? WeightKg { get; set; }
    public int? PulsePerMinute { get; set; }
    public decimal? Hemoglobin { get; set; }
    public decimal? Hematocrit { get; set; }
    public int? PlateletCount { get; set; }
    public string? BloodPressure { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public string? ScreeningStaffName { get; set; }
    public string? ScreeningStaffSignature { get; set; }
    public DateTime? ScreeningDate { get; set; }
    public string? ScreeningTime { get; set; }
    public bool MedicalExamAccepted { get; set; } = true;
    public string? DeferralReason { get; set; }
    public string? MedicalOfficerName { get; set; }
    public string? MedicalOfficerSignature { get; set; }
    public DateTime? MedicalOfficerDate { get; set; }
    public string? DonorNumber { get; set; }
    public string? BagType { get; set; }
    public string? SegmentNumber { get; set; }
    public string? DonationStartTime { get; set; }
    public string? DonationEndTime { get; set; }
    public string? VenipunctureSite { get; set; }
    public string? PhlebotomistName { get; set; }
    public string? PhlebotomistSignature { get; set; }
    public bool VisualInspectionPassed { get; set; } = true;
    public bool DonorConfirmed { get; set; } = true;
    public bool FeelsHealthyToday { get; set; } = true;
    public bool DonatedRecently { get; set; }
    public bool HadDonationDiscomfort { get; set; }
    public bool AteRecently { get; set; } = true;
    public bool SleptWell { get; set; } = true;
    public bool HasInfectionRisk { get; set; }
    public bool HasColdFeverOrSinusitis { get; set; }
    public bool TookAntibioticsRecently { get; set; }
    public bool TookAlcoholRecently { get; set; }
    public bool SmokesFrequently { get; set; }
    public bool TookMedicationRecently { get; set; }
    public bool HadRecentVaccination { get; set; }
    public bool HadRecentIllness { get; set; }
    public bool HadRecentSurgeryOrTattoo { get; set; }
    public bool HadRecentMalariaOrEndemicExposure { get; set; }
    public bool HasChronicDisease { get; set; }
    public bool IsPregnantOrBreastfeeding { get; set; }
    public bool HadRecentAbortion { get; set; }
    public bool HasUnsafeSexualExposure { get; set; }
    public bool HasHivOrHepatitisRisk { get; set; }
    public bool HasPermanentDisqualification { get; set; }
}
