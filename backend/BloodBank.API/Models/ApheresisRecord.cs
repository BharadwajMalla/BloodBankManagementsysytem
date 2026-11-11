using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

/// <summary>
/// Digital Single Donor Platelet (SDP) & Plasma Apheresis Donor Form and Procedure Register
/// Implements "UDDANAM BLOOD CENTRE - SDP DONOR FORM (PLATELET / PLASMA APHERESIS) & SDP ISSUE RECORD"
/// </summary>
public class ApheresisRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)]
    public string RegistrationNo { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(150)]
    public string DonorName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? AgeSex { get; set; }

    public decimal WeightKg { get; set; }

    [Required, MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? TimeOfMeal { get; set; }

    // Patient Requisition Info (In case of directed replacement)
    [MaxLength(150)] public string? PatientName { get; set; }
    [MaxLength(50)] public string? PatientIpNo { get; set; }
    [MaxLength(50)] public string? PatientBedNo { get; set; }
    [MaxLength(10)] public string? PatientBloodGroup { get; set; }

    public bool TakenAspirinLast3Days { get; set; }

    // Vitals & Lab Assessment
    public decimal HaemoglobinGms { get; set; } // g/dL
    public decimal PlateletCountLakhs { get; set; } // Lakhs/cu mm (must be >= 2.0 Lakhs)
    public int WbcCount { get; set; } // < 11000
    [MaxLength(30)] public string? BloodPressure { get; set; }
    public int Pulse { get; set; }
    public decimal TemperatureCelsius { get; set; }
    public decimal TotalProteinsGms { get; set; } // >= 6.0 g%

    // Cell Separator Apheresis Procedure Details
    [MaxLength(20)] public string? ProcedureStartTime { get; set; }
    [MaxLength(20)] public string? ProcedureEndTime { get; set; }
    public int DurationMinutes { get; set; }
    [MaxLength(20)] public string VenipunctureSite { get; set; } = "Right"; // Right / Left
    public decimal WholeBloodPumpRateMlMin { get; set; } = 60.0m;
    [MaxLength(20)] public string AcdRatio { get; set; } = "1:9";
    public decimal TotalBloodVolumeProcessedMl { get; set; } = 3000m;
    public decimal TotalProductCollectedMl { get; set; } = 300m;
    public decimal TotalVolumeAcdUsedMl { get; set; } = 300m;

    [MaxLength(200)] public string? AdverseReaction { get; set; }
    public bool QualityControlPassed { get; set; } = true;

    [MaxLength(150)] public string? PhlebotomistSignature { get; set; }
    [MaxLength(150)] public string? MedicalOfficerSignature { get; set; }
    [MaxLength(500)] public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
