using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class BloodRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequesterId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RequesterType { get; set; } = "Hospital"; // "Hospital" or "Donor"

    [Required]
    [MaxLength(150)]
    public string RequesterName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RequesterContact { get; set; }

    [MaxLength(150)]
    public string? HospitalName { get; set; }

    [Required]
    [MaxLength(150)]
    public string PatientName { get; set; } = string.Empty;

    public int? PatientAge { get; set; }

    [MaxLength(10)]
    public string? PatientAgeUnit { get; set; }

    [MaxLength(20)]
    public string? PatientSex { get; set; }

    [Required]
    [MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty; // "A+", "O-", etc.

    [Required]
    [MaxLength(50)]
    public string ComponentType { get; set; } = "Whole Blood"; // "Whole Blood", "Red Blood Cells (PRBC)", "Platelets", "Fresh Frozen Plasma (FFP)", "Cryoprecipitate", "Single Donor Platelet (SDP)"

    [Range(1, 100)]
    public int QuantityUnits { get; set; } = 1;

    [MaxLength(20)]
    public string Urgency { get; set; } = "Normal"; // "Normal", "Urgent", "Critical"

    [MaxLength(30)]
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Fulfilled", "Rejected"

    [MaxLength(100)]
    public string? HospitalDepartment { get; set; }

    [MaxLength(500)]
    public string? MedicalReason { get; set; }

    [MaxLength(500)]
    public string? AdminComments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(2000)]
    public string? IssuedBagNumbers { get; set; }

    public string? IssuedBagDetailsJson { get; set; }

    [MaxLength(20)]
    public string? PaymentMode { get; set; }

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [MaxLength(100)]
    public string? TransactionId { get; set; }

    public decimal? AmountPaid { get; set; }

    public bool IsThalassemiaPatient { get; set; }

    [MaxLength(50)]
    public string? DiscountDonorId { get; set; }

    public decimal DiscountAmount { get; set; }

    // Uddanam Register Fields
    [MaxLength(100)] public string? Indication { get; set; } // "Surgery", "Therapeutic"
    [MaxLength(100)] public string? EmergencyUrgency { get; set; } // "5 Min: Grouping", "30 Min: Saline X-Match", "2 Hour: Complete X-Match", "Routine"
    [MaxLength(150)] public string? RefDoctor { get; set; }
    [MaxLength(50)] public string? IpNumber { get; set; }
    [MaxLength(50)] public string? RoomNumber { get; set; }
    public bool? PreviousTransfusion { get; set; }
    public bool? TransfusionReactionHistory { get; set; }
    [MaxLength(200)] public string? CrossMatchingReport { get; set; } // "Saline / ALB / AHG Compatible"
    [MaxLength(50)] public string? BillNumber { get; set; }
    [MaxLength(20)] public string? CrossMatchingMethod { get; set; }
    public decimal? HbGdl { get; set; }
    public decimal? PcvPercent { get; set; }
    public decimal? PlateletCount { get; set; }
    [MaxLength(500)] public string? IndicationDiagnosis { get; set; }
    [MaxLength(30)] public string? RequiredResponseTime { get; set; }
    public DateTime? RequiredDateTime { get; set; }
    [MaxLength(20)] public string? IssueOrReserve { get; set; }
    [MaxLength(150)] public string? ConsentTakenBy { get; set; }
    public DateTime? ConsentDate { get; set; }
    public bool ConsentForIssue { get; set; }
    public bool BloodBankUseOnly { get; set; }
    [MaxLength(50)] public string? IssueSerial { get; set; }
    [MaxLength(150)] public string? ReceiverName { get; set; }
    [MaxLength(150)] public string? IssuedByName { get; set; }
    [MaxLength(150)] public string? BloodBankOfficerSignOff { get; set; }
    public int? YearlySlNo { get; set; }
    public int? MonthlySlNo { get; set; }
}
