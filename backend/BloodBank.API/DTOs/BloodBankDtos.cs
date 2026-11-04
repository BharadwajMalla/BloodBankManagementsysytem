using System.ComponentModel.DataAnnotations;

namespace BloodBank.API.DTOs;

public class AddStockDto
{
    [Required]
    [StringLength(50)]
    public string BagNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Blood group is required.")]
    public string BloodGroup { get; set; } = string.Empty;

    [Required(ErrorMessage = "Component type is required.")]
    public string ComponentType { get; set; } = "Whole Blood";

    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
}

public class BloodBagDto
{
    public Guid Id { get; set; }
    public string BagNumber { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int QuantityUnits { get; set; }
    public DateTime AddedDate { get; set; }
}

public class BloodStockDto
{
    public Guid Id { get; set; }
    public string BloodGroup { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int QuantityUnits { get; set; }
    public string BloodBankName { get; set; } = "Blood Bank A";
    public DateTime LastUpdated { get; set; }
    public string Status => QuantityUnits switch
    {
        <= 5 => "Critical Shortage",
        <= 15 => "Low Stock",
        _ => "Adequate"
    };
}

public class CreateCampDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string CampName { get; set; } = string.Empty;

    [Required]
    [StringLength(250, MinimumLength = 3)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime CampDate { get; set; }

    [Range(1, 5000)]
    public int TargetUnits { get; set; } = 50;

    public string ContactPhone { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class ProcessRequestDto
{
    [Required]
    public string Status { get; set; } = "Approved";

    public string? AdminComments { get; set; }
}

public class DonorDetailsDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? BloodGroup { get; set; }
    public DateTime RegisteredDate { get; set; }
    public int TotalDonations { get; set; }
    public DateTime? LastDonationDate { get; set; }
    public bool IsEligibleToDonate => !LastDonationDate.HasValue || (DateTime.UtcNow - LastDonationDate.Value).TotalDays >= 90;
}
