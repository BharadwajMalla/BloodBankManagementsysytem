namespace BloodBank.Application.Models;

public enum MasterDataCategory
{
    Hospital,
    Doctor,
    Staff,
    Product,
    BagType,
    Supplier
}

public sealed class MasterDataEntry
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Specialty { get; set; }
    public string? HospitalName { get; set; }
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? Role { get; set; }
    public int? ShelfLifeDays { get; set; }
    public decimal? VolumeMl { get; set; }
    public string? Anticoagulant { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
