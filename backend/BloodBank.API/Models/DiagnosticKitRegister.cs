using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

/// <summary>
/// Digital Register for Diagnostic Kits and Reagents
/// Implements "UDDANAM BLOOD CENTRE - REGISTER FOR DIAGNOSTIC KITS AND REAGENTS (ELISA RECORD 2026)"
/// </summary>
public class DiagnosticKitRegister
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int SlNo { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string KitName { get; set; } = string.Empty; // e.g. "4th Gen HIV ELISA Kit", "HBsAg ELISA Kit", "Anti-A Antisera", "Anti-B Antisera", "Anti-D Monoclonal"

    [Required, MaxLength(200)]
    public string Manufacturer { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string InvoiceNo { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string BatchNo { get; set; } = string.Empty;

    public DateTime DateOfManufacture { get; set; } = DateTime.UtcNow.AddMonths(-2);
    public DateTime DateOfExpiry { get; set; } = DateTime.UtcNow.AddMonths(12);

    public int QuantityReceived { get; set; }
    public int OpeningBalance { get; set; }
    public int NoOfTests { get; set; }
    public int ControlsUsed { get; set; } // Positive & Negative controls used
    public int Wastage { get; set; }
    public int ClosingBalance { get; set; }

    [MaxLength(150)]
    public string? TechSign { get; set; }

    [MaxLength(150)]
    public string? MedicalOfficerSign { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
