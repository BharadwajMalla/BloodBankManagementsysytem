namespace BloodBank.Application.Dtos;

public sealed class MasterDataCommand
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? RegistrationNumber { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Specialty { get; init; }
    public string? HospitalName { get; init; }
    public string? Category { get; init; }
    public string? Department { get; init; }
    public string? Role { get; init; }
    public int? ShelfLifeDays { get; init; }
    public decimal? VolumeMl { get; init; }
    public string? Anticoagulant { get; init; }
    public string? Description { get; init; }
    public string? Password { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class MasterDataEntryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? RegistrationNumber { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Specialty { get; init; }
    public string? HospitalName { get; init; }
    public string? Category { get; init; }
    public string? Department { get; init; }
    public string? Role { get; init; }
    public int? ShelfLifeDays { get; init; }
    public decimal? VolumeMl { get; init; }
    public string? Anticoagulant { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
