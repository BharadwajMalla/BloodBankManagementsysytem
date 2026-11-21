using BloodBank.Application.Contracts;
using BloodBank.Application.Dtos;
using BloodBank.Application.Models;
using System.Text.RegularExpressions;

namespace BloodBank.Application.Services;

public sealed class MasterDataService(
    IMasterDataRepository repository,
    IPasswordHasher passwordHasher) : IMasterDataService
{
    private static readonly HashSet<string> StaffRoles =
        ["Admin", "BloodBank", "TechnicalSupervisor", "Technician"];
    private static readonly Regex InitialPasswordPattern = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&_\-#])[A-Za-z\d@$!%*?&_\-#]{8,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<IReadOnlyList<MasterDataEntryDto>> GetAsync(
        MasterDataCategory category,
        string? search,
        CancellationToken cancellationToken)
    {
        var records = await repository.GetAsync(category, search?.Trim(), cancellationToken);
        return records.Select(Map).ToList();
    }

    public async Task<MasterDataEntryDto> CreateAsync(
        MasterDataCategory category,
        MasterDataCommand command,
        string actor,
        CancellationToken cancellationToken)
    {
        Validate(category, command, isCreate: true);

        var code = NormalizeCode(category, command);
        var email = NormalizeEmail(command.Email);
        if (await repository.CodeOrEmailExistsAsync(
                category, code, command.RegistrationNumber, email, null, cancellationToken))
            throw new MasterDataConflictException("The code, registration number, or email is already in use.");

        var now = DateTime.UtcNow;
        var entry = ToEntry(category, command, code, email, Guid.NewGuid(), now, actor);
        var passwordHash = NeedsAccount(category)
            ? passwordHasher.HashPassword(command.Password!, 11)
            : null;

        var created = await repository.CreateAsync(category, entry, passwordHash, cancellationToken);
        return Map(created);
    }

    public async Task<MasterDataEntryDto> UpdateAsync(
        MasterDataCategory category,
        Guid id,
        MasterDataCommand command,
        string actor,
        CancellationToken cancellationToken)
    {
        Validate(category, command, isCreate: false);

        var existing = await repository.FindAsync(category, id, cancellationToken)
            ?? throw new MasterDataNotFoundException("The requested master-data record was not found.");
        var code = NormalizeCode(category, command);
        var email = NormalizeEmail(command.Email);
        if (await repository.CodeOrEmailExistsAsync(
                category, code, command.RegistrationNumber, email, id, cancellationToken))
            throw new MasterDataConflictException("The code, registration number, or email is already in use.");

        var entry = ToEntry(category, command, code, email, id, existing.CreatedAt, existing.CreatedBy);
        entry.UpdatedAt = DateTime.UtcNow;
        entry.UpdatedBy = actor;
        entry.IsActive = command.IsActive;

        if (!await repository.UpdateAsync(category, entry, cancellationToken))
            throw new MasterDataNotFoundException("The requested master-data record was not found.");

        return Map(entry);
    }

    public async Task SetActiveAsync(
        MasterDataCategory category,
        Guid id,
        bool isActive,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!await repository.SetActiveAsync(category, id, isActive, actor, DateTime.UtcNow, cancellationToken))
            throw new MasterDataNotFoundException("The requested master-data record was not found.");
    }

    private static void Validate(MasterDataCategory category, MasterDataCommand command, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(NormalizeCode(category, command)))
            throw new ArgumentException("Code is required.");
        if (command.Name.Trim().Length > (category == MasterDataCategory.Supplier ? 200 : 150)
            || NormalizeCode(category, command).Length > 50
            || (command.Email?.Trim().Length ?? 0) > 200
            || (command.Phone?.Trim().Length ?? 0) > (NeedsAccount(category) ? 20 : 30)
            || (command.Address?.Trim().Length ?? 0) > 500
            || (command.RegistrationNumber?.Trim().Length ?? 0) > 100
            || (command.ContactPerson?.Trim().Length ?? 0) > 150
            || (command.HospitalName?.Trim().Length ?? 0) > 150
            || (command.Department?.Trim().Length ?? 0) > 100
            || (command.Role?.Trim().Length ?? 0) > 50
            || (command.Category?.Trim().Length ?? 0) > 100
            || (command.Specialty?.Trim().Length ?? 0) > 100
            || (command.Anticoagulant?.Trim().Length ?? 0) > 100
            || (command.Description?.Trim().Length ?? 0) > 500)
            throw new ArgumentException("One or more master-data fields exceed their allowed length.");

        if (NeedsAccount(category) && string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("Email is required for a login account.");
        if (!string.IsNullOrWhiteSpace(command.Email)
            && !System.Net.Mail.MailAddress.TryCreate(command.Email.Trim(), out _))
        {
            throw new ArgumentException("A valid email address is required.");
        }

        if (category == MasterDataCategory.Product && command.ShelfLifeDays is not > 0)
            throw new ArgumentException("Shelf life must be greater than zero days.");
        if (category == MasterDataCategory.BagType && command.VolumeMl is not > 0)
            throw new ArgumentException("Bag volume must be greater than zero millilitres.");
        if (category == MasterDataCategory.Staff)
        {
            if (string.IsNullOrWhiteSpace(command.Role) || !StaffRoles.Contains(command.Role.Trim()))
                throw new ArgumentException("Select a supported staff login role.");
            if (string.IsNullOrWhiteSpace(command.Department))
                throw new ArgumentException("Department is required for staff.");
        }
        if (NeedsAccount(category) && isCreate
            && (command.Password is null || !InitialPasswordPattern.IsMatch(command.Password)))
            throw new ArgumentException(
                "The initial password must be at least 8 characters and contain uppercase, lowercase, a number, and a special character.");
    }

    private static string NormalizeCode(MasterDataCategory category, MasterDataCommand command) =>
        (category == MasterDataCategory.Hospital
            ? command.RegistrationNumber
            : command.Code)?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static bool NeedsAccount(MasterDataCategory category) =>
        category is MasterDataCategory.Hospital or MasterDataCategory.Staff;

    private static MasterDataEntry ToEntry(
        MasterDataCategory category,
        MasterDataCommand command,
        string code,
        string? email,
        Guid id,
        DateTime createdAt,
        string? createdBy) =>
        new()
        {
            Id = id,
            Code = code,
            Name = command.Name.Trim(),
            RegistrationNumber = category == MasterDataCategory.Hospital
                ? code
                : command.RegistrationNumber?.Trim(),
            ContactPerson = command.ContactPerson?.Trim(),
            Phone = command.Phone?.Trim(),
            Email = email,
            Address = command.Address?.Trim(),
            Specialty = command.Specialty?.Trim(),
            HospitalName = command.HospitalName?.Trim(),
            Category = command.Category?.Trim(),
            Department = command.Department?.Trim(),
            Role = command.Role?.Trim(),
            ShelfLifeDays = command.ShelfLifeDays,
            VolumeMl = command.VolumeMl,
            Anticoagulant = command.Anticoagulant?.Trim(),
            Description = command.Description?.Trim(),
            IsActive = command.IsActive,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };

    private static MasterDataEntryDto Map(MasterDataEntry entry) =>
        new()
        {
            Id = entry.Id,
            Code = entry.Code,
            Name = entry.Name,
            RegistrationNumber = entry.RegistrationNumber,
            ContactPerson = entry.ContactPerson,
            Phone = entry.Phone,
            Email = entry.Email,
            Address = entry.Address,
            Specialty = entry.Specialty,
            HospitalName = entry.HospitalName,
            Category = entry.Category,
            Department = entry.Department,
            Role = entry.Role,
            ShelfLifeDays = entry.ShelfLifeDays,
            VolumeMl = entry.VolumeMl,
            Anticoagulant = entry.Anticoagulant,
            Description = entry.Description,
            IsActive = entry.IsActive,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            CreatedBy = entry.CreatedBy,
            UpdatedBy = entry.UpdatedBy
        };
}
