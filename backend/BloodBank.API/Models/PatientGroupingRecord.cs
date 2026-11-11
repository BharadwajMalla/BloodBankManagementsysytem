using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

/// <summary>
/// Digital Patient Blood Grouping & Crossmatch Register
/// Implements "UDDANAM BLOOD CENTRE - PATIENT BLOOD GROUPING RECORD 2026"
/// </summary>
public class PatientGroupingRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int SlNo { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(150)]
    public string PatientName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? AgeSex { get; set; }

    [MaxLength(150)]
    public string? HospitalName { get; set; }

    [MaxLength(50)]
    public string? IpNumber { get; set; }

    [MaxLength(50)]
    public string? BagNumber { get; set; }

    [MaxLength(50)]
    public string? BagType { get; set; }

    [Required, MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string RhType { get; set; } = "Positive";

    // Forward Grouping
    [MaxLength(10)] public string AntiA { get; set; } = "+";
    [MaxLength(10)] public string AntiB { get; set; } = "-";
    [MaxLength(10)] public string AntiAb { get; set; } = "+";
    [MaxLength(10)] public string AntiD { get; set; } = "+";
    [MaxLength(10)] public string LectinA1 { get; set; } = "+";
    [MaxLength(10)] public string LectinH { get; set; } = "-";

    // Reverse / Serum Grouping
    [MaxLength(10)] public string ACells { get; set; } = "-";
    [MaxLength(10)] public string BCells { get; set; } = "+";
    [MaxLength(10)] public string OCells { get; set; } = "-";

    // Special Tests
    [MaxLength(20)] public string DuTest { get; set; } = "Negative";
    [MaxLength(20)] public string IrregularAntibodies { get; set; } = "Negative";
    [MaxLength(20)] public string AutoAntibodies { get; set; } = "Negative";

    // Cross-Matching
    [MaxLength(30)] public string CrossmatchSaline { get; set; } = "Compatible";
    [MaxLength(30)] public string CrossmatchAlbumin { get; set; } = "Compatible";
    [MaxLength(30)] public string CrossmatchAhg { get; set; } = "Compatible";

    [MaxLength(150)] public string? TechnicianSignature { get; set; }
    [MaxLength(150)] public string? MedicalOfficerSignature { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
