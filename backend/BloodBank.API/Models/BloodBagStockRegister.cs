using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

/// <summary>
/// Digital Stock Register for Blood Bags & Consumables
/// Implements "UDDANAM BLOOD CENTRE - STOCK REGISTER FOR BLOOD BAGS (CPDA BAGS 2026 TO 2027)"
/// </summary>
public class BloodBagStockRegister
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int SlNo { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string InvoiceNo { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(200)]
    public string Manufacturer { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string BatchNo { get; set; } = string.Empty;

    public DateTime DateOfSupply { get; set; } = DateTime.UtcNow;
    public DateTime DateOfManufacture { get; set; } = DateTime.UtcNow.AddMonths(-3);
    public DateTime DateOfExpiry { get; set; } = DateTime.UtcNow.AddYears(2);

    [Required, MaxLength(100)]
    public string CodeSpecification { get; set; } = "CPDA-1 Triple (450ml)"; // e.g. Single 350ml, Double 450ml, Triple 450ml, Quadruple SAGM

    [MaxLength(50)]
    public string ResultCoa { get; set; } = "Satisfactory"; // Certificate of Analysis verification

    public int OpeningStock { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityUsed { get; set; }
    public int Wastage { get; set; }
    public int Balance { get; set; }

    [MaxLength(150)]
    public string? TechSign { get; set; }

    [MaxLength(150)]
    public string? MedicalOfficerSign { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
