using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class AddStockDto
{
    [Required]
    [StringLength(50)]
    public string BagNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Blood group is required.")]
    public string BloodGroup { get; set; } = string.Empty;

    [Required(ErrorMessage = "Component type is required.")]
    public string ComponentType { get; set; } = "Whole Blood";

    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
}

public class CreateComponentCollectionDto
{
    [Required] public string ComponentType { get; set; } = string.Empty;
    [Required] public string BloodGroup { get; set; } = string.Empty;
    [Required] public string BagType { get; set; } = string.Empty;
    [Range(0.1, 100000)] public decimal VolumeMl { get; set; }
    [Required] public string BagBarcode { get; set; } = string.Empty;
    [Required] public DateTime CollectionDate { get; set; }
    [Required] public DateTime ExpiryDate { get; set; }
}

public class ComponentCollectionDto : CreateComponentCollectionDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Available";
}

public class CreateComponentDiscardDto
{
    [Required] public string BagBarcode { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
    [Required, StringLength(150)] public string DiscardedBy { get; set; } = string.Empty;
}

public class ComponentDiscardDto : CreateComponentDiscardDto
{
    public Guid Id { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class CreateComponentReturnDto
{
    [Required] public string BagBarcode { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
    [Required, StringLength(150)] public string ReturnedBy { get; set; } = string.Empty;
}

public class ComponentReturnDto : CreateComponentReturnDto
{
    public Guid Id { get; set; }
    public DateTime ReturnDate { get; set; }
}

public class BloodBagDto
{
    public Guid Id { get; set; }
    public string BagNumber { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int QuantityUnits { get; set; }
    public DateTime AddedDate { get; set; }
}

public class BloodStockDto
{
    public Guid Id { get; set; }
    public string BloodGroup { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int QuantityUnits { get; set; }
    public string BloodBankName { get; set; } = "Blood Bank A";
    public DateTime LastUpdated { get; set; }
    public string Status => QuantityUnits switch
    {
        <= 5 => "Critical Shortage",
        <= 15 => "Low Stock",
        _ => "Adequate"
    };
}

public class CreateCampDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string CampName { get; set; } = string.Empty;

    [Required]
    [StringLength(250, MinimumLength = 3)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime CampDate { get; set; }

    [Range(1, 5000)]
    public int TargetUnits { get; set; } = 50;

    public string ContactPhone { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class ProcessRequestDto
{
    [Required]
    public string Status { get; set; } = "Approved";

    public string? AdminComments { get; set; }

    [RegularExpression("^(Cash|UPI)$")]
    public string? PaymentMode { get; set; }

    public string? TransactionId { get; set; }

    public decimal? AmountPaid { get; set; }
}

public class DonorDetailsDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Sex { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public DateTime RegisteredDate { get; set; }
    public int TotalDonations { get; set; }
    public DateTime? LastDonationDate { get; set; }
    public bool IsEligibleToDonate
    {
        get
        {
            var age = DateOfBirth.HasValue
                ? DateTime.UtcNow.Year - DateOfBirth.Value.Year -
                  (DateTime.UtcNow.Date < DateOfBirth.Value.Date.AddYears(DateTime.UtcNow.Year - DateOfBirth.Value.Year) ? 1 : 0)
                : 0;
            return age is >= 18 and <= 65 &&
                   (!LastDonationDate.HasValue || (DateTime.UtcNow - LastDonationDate.Value).TotalDays >= 90);
        }
    }
    public string EligibilityStatus
    {
        get
        {
            var age = DateOfBirth.HasValue
                ? DateTime.UtcNow.Year - DateOfBirth.Value.Year -
                  (DateTime.UtcNow.Date < DateOfBirth.Value.Date.AddYears(DateTime.UtcNow.Year - DateOfBirth.Value.Year) ? 1 : 0)
                : 0;
            if (age < 18 || age > 65)
                return "Age restriction";
            return LastDonationDate.HasValue && (DateTime.UtcNow - LastDonationDate.Value).TotalDays < 90
                ? "Cooldown Active"
                : "Eligible to Donate";
        }
    }
    public string DonationStatus => TotalDonations > 0 ? "Donated" : "Not Donated";
}

public class CreateDonorDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(75, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(75, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Sex is required.")]
    [StringLength(20)]
    public string Sex { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Blood group is required.")]
    [StringLength(10)]
    public string BloodGroup { get; set; } = string.Empty;
}

public class CreateDonorResponseDto
{
    public DonorDetailsDto Donor { get; set; } = new();
    public string GeneratedPassword { get; set; } = string.Empty;
}

public class CompleteDonationDto
{
    public bool DonorConfirmed { get; set; }
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
    public bool MedicalExamAccepted { get; set; }
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
    public bool VisualInspectionPassed { get; set; }
    public bool FeelsHealthyToday { get; set; }
    public bool DonatedRecently { get; set; }
    public bool HadDonationDiscomfort { get; set; }
    public bool AteRecently { get; set; }
    public bool SleptWell { get; set; }
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
    public int UnitsDonated { get; set; } = 1;
    public string? CampName { get; set; }
}

public class DonationCompletionResultDto
{
    public bool IsEligible { get; set; }
    public string Message { get; set; } = string.Empty;
    public DonorDetailsDto? Donor { get; set; }
}

public class DonationHistoryDto
{
    public Guid Id { get; set; }
    public Guid DonorId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public int UnitsDonated { get; set; }
    public DateTime DonationDate { get; set; }
    public string? CampName { get; set; }
    public string BloodBankName { get; set; } = string.Empty;
    public decimal? WeightKg { get; set; }
    public int? PulsePerMinute { get; set; }
    public decimal? Hemoglobin { get; set; }
    public decimal? Hematocrit { get; set; }
    public int? PlateletCount { get; set; }
    public string? BloodPressure { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public string? ScreeningStaffName { get; set; }
    public DateTime? ScreeningDate { get; set; }
    public string? ScreeningTime { get; set; }
    public bool MedicalExamAccepted { get; set; }
    public string? MedicalOfficerName { get; set; }
    public DateTime? MedicalOfficerDate { get; set; }
    public string? DonorNumber { get; set; }
    public string? BagType { get; set; }
    public string? SegmentNumber { get; set; }
    public string? DonationStartTime { get; set; }
    public string? DonationEndTime { get; set; }
    public string? VenipunctureSite { get; set; }
    public string? PhlebotomistName { get; set; }
    public bool VisualInspectionPassed { get; set; }
    public bool DonorConfirmed { get; set; }
    public bool FeelsHealthyToday { get; set; }
    public bool DonatedRecently { get; set; }
    public bool HadDonationDiscomfort { get; set; }
    public bool AteRecently { get; set; }
    public bool SleptWell { get; set; }
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
