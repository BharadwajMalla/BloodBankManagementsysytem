using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

/// <summary>
/// Digital Transfusion-Transmissible Infection (TTI) Mandatory Screening Register
/// Implements "UDDANAM BLOOD CENTRE - SCREENING REGISTER" (HIV, HBsAg, HCV, VDRL, Malaria, Atypical Ab)
/// </summary>
public class TtiScreeningRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)]
    public string BagBarcode { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty;

    public DateTime TestingDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    public string HivResult { get; set; } = "Non-Reactive"; // Non-Reactive, Reactive

    [Required, MaxLength(20)]
    public string HbsAgResult { get; set; } = "Non-Reactive"; // Non-Reactive, Reactive

    [Required, MaxLength(20)]
    public string HcvResult { get; set; } = "Non-Reactive"; // Non-Reactive, Reactive

    [Required, MaxLength(20)]
    public string VdrlResult { get; set; } = "Non-Reactive"; // Non-Reactive, Reactive

    [Required, MaxLength(20)]
    public string MalariaResult { get; set; } = "Negative"; // Negative, Positive

    [Required, MaxLength(20)]
    public string AtypicalAntibodies { get; set; } = "Negative"; // Negative, Positive

    [Required, MaxLength(30)]
    public string OverallStatus { get; set; } = "Passed"; // Passed, Quarantine, Discarded

    [MaxLength(150)]
    public string TestedBy { get; set; } = "Lab Technician";

    [MaxLength(150)]
    public string? ApprovedBy { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
