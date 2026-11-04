using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class BloodBag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string BagNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string BloodGroup { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ComponentType { get; set; } = "Whole Blood";

    [Range(1, 100000)]
    public int QuantityUnits { get; set; } = 1;

    public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    public bool IsIssued { get; set; }

    public DateTime? IssuedDate { get; set; }
}
