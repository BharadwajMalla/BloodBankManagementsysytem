using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class DonationRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DonorId { get; set; }

    [Required]
    [MaxLength(150)]
    public string DonorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty;

    public int UnitsDonated { get; set; } = 1;

    public DateTime DonationDate { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? CampName { get; set; } // If donated at a specific camp

    [MaxLength(50)]
    public string BloodBankName { get; set; } = "Blood Bank A";

    // Uddanam Register Numbering & Categorization
    public int? YearlySlNo { get; set; }
    public int? MonthlySlNo { get; set; }
    [MaxLength(30)] public string DonationCategory { get; set; } = "Voluntary"; // Voluntary / Replacement
    [MaxLength(150)] public string? ReplacementPatientName { get; set; }
    [MaxLength(150)] public string? ReplacementPatientHospital { get; set; }
    [MaxLength(50)] public string? ReplacementPatientIpNo { get; set; }
    [MaxLength(50)] public string? BagNumber { get; set; }
    [MaxLength(50)] public string? HbTestMethod { get; set; } = "Hemocue"; // Copper Sulphate, Hemocue, Automated Cell Counter
    [MaxLength(30)] public string? HbEligibilityStatus { get; set; } = "Fit"; // Fit / Unfit
    [MaxLength(500)] public string? DeferralRecords { get; set; }

    public decimal? WeightKg { get; set; }
    public int? PulsePerMinute { get; set; }
    public decimal? Hemoglobin { get; set; }
    public decimal? Hematocrit { get; set; }
    public int? PlateletCount { get; set; }
    [MaxLength(30)] public string? BloodPressure { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    [MaxLength(150)] public string? ScreeningStaffName { get; set; }
    [MaxLength(200)] public string? ScreeningStaffSignature { get; set; }
    public DateTime? ScreeningDate { get; set; }
    [MaxLength(20)] public string? ScreeningTime { get; set; }
    public bool MedicalExamAccepted { get; set; } = true;
    [MaxLength(500)] public string? DeferralReason { get; set; }
    [MaxLength(150)] public string? MedicalOfficerName { get; set; }
    [MaxLength(200)] public string? MedicalOfficerSignature { get; set; }
    public DateTime? MedicalOfficerDate { get; set; }
    [MaxLength(100)] public string? DonorNumber { get; set; }
    [MaxLength(50)] public string? BagType { get; set; }
    [MaxLength(100)] public string? SegmentNumber { get; set; }
    [MaxLength(20)] public string? DonationStartTime { get; set; }
    [MaxLength(20)] public string? DonationEndTime { get; set; }
    [MaxLength(50)] public string? VenipunctureSite { get; set; }
    [MaxLength(150)] public string? PhlebotomistName { get; set; }
    [MaxLength(200)] public string? PhlebotomistSignature { get; set; }
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
