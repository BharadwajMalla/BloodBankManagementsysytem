using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class CreateBloodRequestDto : IValidatableObject
{
    [StringLength(150)]
    public string? HospitalName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    [Range(0, 1440)]
    public int? PatientAge { get; set; }

    [Required]
    [RegularExpression("^(Years|Months)$")]
    public string PatientAgeUnit { get; set; } = "Years";

    [Required]
    [RegularExpression("^(Male|Female|Other|Unknown)$")]
    public string PatientSex { get; set; } = string.Empty;

    public bool? IsThalassemiaPatient { get; set; }

    [Required]
    [RegularExpression("^(A\\+|A\\-|B\\+|B\\-|AB\\+|AB\\-|O\\+|O\\-)$")]
    public string BloodGroup { get; set; } = string.Empty;

    [Required]
    public string ComponentType { get; set; } = "Whole Blood"; // Whole Blood, Red Blood Cells (PRBC), Platelets, Fresh Frozen Plasma (FFP), Cryoprecipitate

    [Range(1, 100)]
    public int QuantityUnits { get; set; } = 1;

    [RegularExpression("^(Normal|Urgent|Critical)$")]
    public string Urgency { get; set; } = "Normal";

    public string? HospitalDepartment { get; set; }

    public string? MedicalReason { get; set; }
    public string? Indication { get; set; }
    public string? EmergencyUrgency { get; set; }
    public string? RefDoctor { get; set; }
    public string? IpNumber { get; set; }
    public string? RoomNumber { get; set; }
    public bool? PreviousTransfusion { get; set; }
    public bool? TransfusionReactionHistory { get; set; }
    public decimal? HbGdl { get; set; }
    public decimal? PcvPercent { get; set; }
    public decimal? PlateletCount { get; set; }
    public string? IndicationDiagnosis { get; set; }
    public string? CrossMatchingMethod { get; set; }
    public string? CrossMatchingReport { get; set; }
    public string? BillNumber { get; set; }
    public string? RequiredResponseTime { get; set; }
    public DateTime? RequiredDateTime { get; set; }
    public string? IssueOrReserve { get; set; }
    public bool ConsentForIssue { get; set; }
    public string? ConsentTakenBy { get; set; }
    public DateTime? ConsentDate { get; set; }
    public bool BloodBankUseOnly { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PatientAge.HasValue &&
            ((PatientAgeUnit == "Years" && PatientAge > 120) ||
             (PatientAgeUnit == "Months" && PatientAge > 1440)))
        {
            yield return new ValidationResult(
                PatientAgeUnit == "Months"
                    ? "Patient age in months must be between 0 and 1440."
                    : "Patient age in years must be between 0 and 120.",
                new[] { nameof(PatientAge) });
        }
    }
}

public class UpdateBloodRequestDto : CreateBloodRequestDto
{
    public string? Status { get; set; }
    public string? AdminComments { get; set; }
    public string? ReceiverName { get; set; }
    public string? IssueSerial { get; set; }
    public string? IssuedByName { get; set; }
    public string? BloodBankOfficerSignOff { get; set; }
}

public class BloodRequestResponseDto
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterType { get; set; } = string.Empty; // "Hospital" or "Donor"
    public string RequesterName { get; set; } = string.Empty;
    public string? RequesterContact { get; set; }
    public string? HospitalName { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public string? PatientAgeUnit { get; set; }
    public string? PatientSex { get; set; }
    public bool IsThalassemiaPatient { get; set; }
    public string BloodGroup { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int QuantityUnits { get; set; }
    public string Urgency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? HospitalDepartment { get; set; }
    public string? MedicalReason { get; set; }
    public string? AdminComments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? IssuedBagNumbers { get; set; }
    public List<IssuedBloodBagDto> IssuedBags { get; set; } = new();
    public string? PaymentMode { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public string? TransactionId { get; set; }
    public decimal? AmountPaid { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountDonorId { get; set; }
    public string? Indication { get; set; }
    public string? EmergencyUrgency { get; set; }
    public string? RefDoctor { get; set; }
    public string? IpNumber { get; set; }
    public decimal? HbGdl { get; set; }
    public decimal? PcvPercent { get; set; }
    public decimal? PlateletCount { get; set; }
    public string? IndicationDiagnosis { get; set; }
    public string? RequiredResponseTime { get; set; }
    public DateTime? RequiredDateTime { get; set; }
    public string? IssueOrReserve { get; set; }
    public string? CrossMatchingMethod { get; set; }
    public string? CrossMatchingReport { get; set; }
    public string? BillNumber { get; set; }
    public bool ConsentForIssue { get; set; }
    public string? ConsentTakenBy { get; set; }
    public DateTime? ConsentDate { get; set; }
    public bool BloodBankUseOnly { get; set; }
    public string? IssueSerial { get; set; }
    public string? ReceiverName { get; set; }
    public string? IssuedByName { get; set; }
    public string? BloodBankOfficerSignOff { get; set; }
}

public class IssuedBloodBagDto
{
    public string BagNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
}
