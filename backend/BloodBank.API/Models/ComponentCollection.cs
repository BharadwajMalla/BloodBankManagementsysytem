using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class ComponentCollection
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string ComponentType { get; set; } = string.Empty;
    [Required, MaxLength(10)] public string BloodGroup { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string BagType { get; set; } = string.Empty;
    public decimal VolumeMl { get; set; }
    [Required, MaxLength(50)] public string BagBarcode { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "Available";
}
