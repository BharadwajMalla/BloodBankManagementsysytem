using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class CreateBloodRequestDto
{
    [StringLength(150)]
    public string? HospitalName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string PatientName { get; set; } = string.Empty;

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
    public string? PaymentMode { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public string? TransactionId { get; set; }
    public decimal? AmountPaid { get; set; }
}
