using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.Models;

public class ComponentDiscard
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string BagBarcode { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string DiscardedBy { get; set; } = string.Empty;
    [MaxLength(50)] public string ApprovalStatus { get; set; } = "Pending";
    [MaxLength(150)] public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
}
