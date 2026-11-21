namespace BloodBank.Application.Dtos;

public sealed record CreateDonorCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateTime DateOfBirth,
    string Sex,
    string Address,
    string BloodGroup);

public sealed class DonorDetailsDto
{
    public Guid Id { get; init; }
    public string DonorId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Sex { get; init; }
    public string? Address { get; init; }
    public string? BloodGroup { get; init; }
    public DateTime RegisteredDate { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
    public int TotalDonations { get; init; }
    public DateTime? LastDonationDate { get; init; }

    public bool IsEligibleToDonate
    {
        get
        {
            var age = GetAge();
            return age is >= 18 and <= 65
                && (!LastDonationDate.HasValue || (DateTime.UtcNow - LastDonationDate.Value).TotalDays >= 90);
        }
    }

    public string EligibilityStatus
    {
        get
        {
            var age = GetAge();
            if (age < 18 || age > 65)
                return "Age restriction";
            return LastDonationDate.HasValue && (DateTime.UtcNow - LastDonationDate.Value).TotalDays < 90
                ? "Cooldown Active"
                : "Eligible to Donate";
        }
    }

    public string DonationStatus => TotalDonations > 0 ? "Donated" : "Not Donated";

    private int GetAge()
    {
        if (!DateOfBirth.HasValue)
            return 0;

        var today = DateTime.UtcNow.Date;
        var age = today.Year - DateOfBirth.Value.Year;
        return today < DateOfBirth.Value.Date.AddYears(age) ? age - 1 : age;
    }
}

public sealed class CreateDonorResponseDto
{
    public DonorDetailsDto Donor { get; init; } = new();
    public string GeneratedPassword { get; init; } = string.Empty;
}
