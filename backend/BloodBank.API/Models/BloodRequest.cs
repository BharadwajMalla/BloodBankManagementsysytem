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

    [Required]
    [MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty; // "A+", "O-", etc.

    [Required]
    [MaxLength(50)]
    public string ComponentType { get; set; } = "Whole Blood"; // "Whole Blood", "Red Blood Cells (PRBC)", "Platelets", "Fresh Frozen Plasma (FFP)", "Cryoprecipitate"

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
}
