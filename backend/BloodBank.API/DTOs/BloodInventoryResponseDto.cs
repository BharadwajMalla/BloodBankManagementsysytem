namespace BloodBank.API.DTOs;

public class BloodInventoryResponseDto
{
    public Guid Id { get; set; }
    public string BloodGroup { get; set; } = string.Empty;
    public int QuantityUnits { get; set; }
    public string BloodBankName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public string Status => QuantityUnits switch
    {
        <= 5 => "Critical Shortage",
        <= 15 => "Low Stock",
        _ => "Adequate"
    };
}
