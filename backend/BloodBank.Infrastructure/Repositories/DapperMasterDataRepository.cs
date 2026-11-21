using System.Data;
using System.Data.Common;
using Dapper;
using BloodBank.Application.Contracts;
using BloodBank.Application.Models;

namespace BloodBank.Infrastructure.Repositories;

public sealed class DapperMasterDataRepository(
    Func<IDbConnection> createConnection,
    bool isSqlite) : IMasterDataRepository
{
    public async Task<IReadOnlyList<MasterDataEntry>> GetAsync(
        MasterDataCategory category,
        string? search,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        var (select, searchColumns) = GetQuery(category);
        var where = searchColumns.Length == 0
            ? string.Empty
            : $" AND (@Search IS NULL OR {string.Join(" OR ", searchColumns.Select(column => $"{column} LIKE @Pattern"))})";
        var sql = $"{select}{where} ORDER BY Name";
        var rows = await connection.QueryAsync<MasterDataEntry>(new CommandDefinition(
            sql,
            new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search,
                Pattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%"
            },
            cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<MasterDataEntry?> FindAsync(
        MasterDataCategory category,
        Guid id,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        var (select, _) = GetQuery(category);
        var idColumn = category == MasterDataCategory.Staff ? "u.Id" : "Id";
        var row = await connection.QuerySingleOrDefaultAsync<MasterDataEntry>(
            new CommandDefinition(
                $"{select} AND {idColumn} = @Id",
                new { Id = id },
                cancellationToken: cancellationToken));
        return NormalizeEntry(category, row);
    }

    public async Task<bool> CodeOrEmailExistsAsync(
        MasterDataCategory category,
        string code,
        string? registrationNumber,
        string? email,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        var sql = category switch
        {
            MasterDataCategory.Hospital =>
                "SELECT COUNT(1) FROM Users WHERE Role = 'Hospital' AND UPPER(COALESCE(HospitalRegNumber,'')) = @Code AND (@ExceptId IS NULL OR Id <> @ExceptId)",
            MasterDataCategory.Doctor =>
                "SELECT COUNT(1) FROM Doctors WHERE (UPPER(Code) = @Code OR (@RegistrationNumber IS NOT NULL AND UPPER(RegistrationNumber) = UPPER(@RegistrationNumber))) AND (@ExceptId IS NULL OR Id <> @ExceptId)",
            MasterDataCategory.Staff =>
                "SELECT COUNT(1) FROM StaffMembers WHERE UPPER(EmployeeNumber) = @Code AND (@ExceptId IS NULL OR UserId <> @ExceptId)",
            MasterDataCategory.Product =>
                "SELECT COUNT(1) FROM Products WHERE UPPER(Code) = @Code AND (@ExceptId IS NULL OR Id <> @ExceptId)",
            MasterDataCategory.BagType =>
                "SELECT COUNT(1) FROM BagTypes WHERE UPPER(Code) = @Code AND (@ExceptId IS NULL OR Id <> @ExceptId)",
            MasterDataCategory.Supplier =>
                "SELECT COUNT(1) FROM Suppliers WHERE UPPER(Code) = @Code AND (@ExceptId IS NULL OR Id <> @ExceptId)",
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new
            {
                Code = code.ToUpperInvariant(),
                RegistrationNumber = registrationNumber,
                ExceptId = exceptId
            },
            cancellationToken: cancellationToken)) > 0;

        if (exists || string.IsNullOrWhiteSpace(email)
            || category is not (MasterDataCategory.Hospital or MasterDataCategory.Staff))
            return exists;

        var emailExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(1) FROM Users
              WHERE LOWER(Email) = LOWER(@Email)
                AND (@ExceptId IS NULL OR Id <> @ExceptId)",
            new { Email = email, ExceptId = exceptId },
            cancellationToken: cancellationToken)) > 0;
        return emailExists;
    }

    public async Task<MasterDataEntry> CreateAsync(
        MasterDataCategory category,
        MasterDataEntry entry,
        string? passwordHash,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        if (category is MasterDataCategory.Hospital or MasterDataCategory.Staff)
        {
            await OpenAsync(connection, cancellationToken);
            using var transaction = connection.BeginTransaction();
            await InsertAccountAsync(connection, transaction, category, entry, passwordHash!, cancellationToken);
            transaction.Commit();
            return entry;
        }

        var sql = category switch
        {
            MasterDataCategory.Doctor => @"
                INSERT INTO Doctors
                    (Id, Code, Name, RegistrationNumber, Specialty, HospitalName, Phone, Email,
                     IsActive, CreatedAt, CreatedBy)
                VALUES
                    (@Id, @Code, @Name, @RegistrationNumber, @Specialty, @HospitalName, @Phone, @Email,
                     @IsActive, @CreatedAt, @CreatedBy)",
            MasterDataCategory.Product => @"
                INSERT INTO Products
                    (Id, Code, Name, Category, ShelfLifeDays, Description, IsActive, CreatedAt, CreatedBy)
                VALUES
                    (@Id, @Code, @Name, @Category, @ShelfLifeDays, @Description, @IsActive, @CreatedAt, @CreatedBy)",
            MasterDataCategory.BagType => @"
                INSERT INTO BagTypes
                    (Id, Code, Name, VolumeMl, Anticoagulant, Description, IsActive, CreatedAt, CreatedBy)
                VALUES
                    (@Id, @Code, @Name, @VolumeMl, @Anticoagulant, @Description, @IsActive, @CreatedAt, @CreatedBy)",
            MasterDataCategory.Supplier => @"
                INSERT INTO Suppliers
                    (Id, Code, Name, ContactPerson, Phone, Email, Address, IsActive, CreatedAt, CreatedBy)
                VALUES
                    (@Id, @Code, @Name, @ContactPerson, @Phone, @Email, @Address, @IsActive, @CreatedAt, @CreatedBy)",
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
        await connection.ExecuteAsync(new CommandDefinition(sql, entry, cancellationToken: cancellationToken));
        return entry;
    }

    public async Task<bool> UpdateAsync(
        MasterDataCategory category,
        MasterDataEntry entry,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        if (category is MasterDataCategory.Hospital or MasterDataCategory.Staff)
        {
            await OpenAsync(connection, cancellationToken);
            using var transaction = connection.BeginTransaction();
            var affected = await UpdateAccountAsync(connection, transaction, category, entry, cancellationToken);
            return affected;
        }

        var sql = category switch
        {
            MasterDataCategory.Doctor => @"
                UPDATE Doctors
                SET Code = @Code, Name = @Name, RegistrationNumber = @RegistrationNumber,
                    Specialty = @Specialty, HospitalName = @HospitalName, Phone = @Phone,
                    Email = @Email, IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id",
            MasterDataCategory.Product => @"
                UPDATE Products
                SET Code = @Code, Name = @Name, Category = @Category, ShelfLifeDays = @ShelfLifeDays,
                    Description = @Description, IsActive = @IsActive, UpdatedAt = @UpdatedAt,
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id",
            MasterDataCategory.BagType => @"
                UPDATE BagTypes
                SET Code = @Code, Name = @Name, VolumeMl = @VolumeMl, Anticoagulant = @Anticoagulant,
                    Description = @Description, IsActive = @IsActive, UpdatedAt = @UpdatedAt,
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id",
            MasterDataCategory.Supplier => @"
                UPDATE Suppliers
                SET Code = @Code, Name = @Name, ContactPerson = @ContactPerson, Phone = @Phone,
                    Email = @Email, Address = @Address, IsActive = @IsActive, UpdatedAt = @UpdatedAt,
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id",
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
        return await connection.ExecuteAsync(new CommandDefinition(
            sql, entry, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> SetActiveAsync(
        MasterDataCategory category,
        Guid id,
        bool isActive,
        string actor,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        using var connection = createConnection();
        if (category == MasterDataCategory.Staff)
            await OpenAsync(connection, cancellationToken);
        var sql = category switch
        {
            MasterDataCategory.Hospital => @"
                UPDATE Users SET IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @Actor
                WHERE Id = @Id AND Role = 'Hospital'",
            MasterDataCategory.Staff => @"
                UPDATE Users SET IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @Actor
                WHERE Id = @Id AND Role IN ('Admin', 'BloodBank', 'TechnicalSupervisor', 'Technician')",
            MasterDataCategory.Doctor => @"
                UPDATE Doctors SET IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @Actor
                WHERE Id = @Id",
            MasterDataCategory.Product => @"
                UPDATE Products SET IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @Actor
                WHERE Id = @Id",
            MasterDataCategory.BagType => @"
                UPDATE BagTypes SET IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @Actor
                WHERE Id = @Id",
            MasterDataCategory.Supplier => @"
                UPDATE Suppliers SET IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @Actor
                WHERE Id = @Id",
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
        using var transaction = category == MasterDataCategory.Staff
            ? connection.BeginTransaction()
            : null;
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, IsActive = isActive, UpdatedAt = updatedAt, Actor = actor },
            transaction,
            cancellationToken: cancellationToken));

        if (affected > 0 && category == MasterDataCategory.Staff)
        {
            await UpsertStaffProfileAsync(
                connection,
                transaction,
                id,
                null,
                null,
                isActive,
                actor,
                updatedAt,
                cancellationToken);
        }

        transaction?.Commit();
        return affected > 0;
    }

    private async Task InsertAccountAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        MasterDataCategory category,
        MasterDataEntry entry,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        var role = category == MasterDataCategory.Hospital ? "Hospital" : entry.Role!;
        await connection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO Users
                (Id, FullName, Email, PasswordHash, Role, Phone, Address, HospitalName, HospitalRegNumber,
                 IsEmailVerified, IsActive, CreatedAt, CreatedBy)
              VALUES
                (@Id, @FullName, @Email, @PasswordHash, @Role, @Phone, @Address, @HospitalName,
                 @HospitalRegNumber, 1, @IsActive, @CreatedAt, @CreatedBy)",
            new
            {
                entry.Id,
                FullName = entry.ContactPerson ?? entry.Name,
                entry.Email,
                PasswordHash = passwordHash,
                Role = role,
                entry.Phone,
                entry.Address,
                HospitalName = category == MasterDataCategory.Hospital ? entry.Name : null,
                HospitalRegNumber = category == MasterDataCategory.Hospital ? entry.RegistrationNumber : null,
                entry.IsActive,
                entry.CreatedAt,
                entry.CreatedBy
            },
            transaction,
            cancellationToken: cancellationToken));

        if (category == MasterDataCategory.Staff)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                @"INSERT INTO StaffMembers
                    (Id, UserId, EmployeeNumber, Department, IsActive, CreatedAt, CreatedBy)
                  VALUES
                    (@Id, @UserId, @EmployeeNumber, @Department, @IsActive, @CreatedAt, @CreatedBy)",
                new
                {
                    Id = Guid.NewGuid(),
                    UserId = entry.Id,
                    EmployeeNumber = entry.Code,
                    entry.Department,
                    entry.IsActive,
                    entry.CreatedAt,
                    entry.CreatedBy
                },
                transaction,
                cancellationToken: cancellationToken));
        }
    }

    private async Task<bool> UpdateAccountAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        MasterDataCategory category,
        MasterDataEntry entry,
        CancellationToken cancellationToken)
    {
        var sql = category == MasterDataCategory.Hospital
            ? @"
                UPDATE Users
                SET FullName = @ContactPerson, Email = @Email, Phone = @Phone, Address = @Address,
                    HospitalName = @Name, HospitalRegNumber = @RegistrationNumber, IsActive = @IsActive,
                    UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND Role = 'Hospital'"
            : @"
                UPDATE Users
                SET FullName = @Name, Email = @Email, Phone = @Phone, Role = @Role,
                    IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND Role IN ('Admin', 'BloodBank', 'TechnicalSupervisor', 'Technician')";
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                entry.Id,
                entry.Name,
                ContactPerson = entry.ContactPerson ?? entry.Name,
                entry.Email,
                entry.Phone,
                entry.Address,
                entry.RegistrationNumber,
                entry.Role,
                entry.IsActive,
                entry.UpdatedAt,
                entry.UpdatedBy
            },
            transaction,
            cancellationToken: cancellationToken));
        if (affected == 0)
            return false;

        if (category == MasterDataCategory.Staff)
        {
            await UpsertStaffProfileAsync(
                connection,
                transaction,
                entry.Id,
                entry.Code,
                entry.Department,
                entry.IsActive,
                entry.UpdatedBy ?? "System",
                entry.UpdatedAt ?? DateTime.UtcNow,
                cancellationToken);
        }

        transaction.Commit();
        return true;
    }

    private async Task UpsertStaffProfileAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid userId,
        string? employeeNumber,
        string? department,
        bool isActive,
        string actor,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var updated = await connection.ExecuteAsync(new CommandDefinition(
            @"UPDATE StaffMembers
              SET EmployeeNumber = COALESCE(@EmployeeNumber, EmployeeNumber),
                  Department = COALESCE(@Department, Department),
                  IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @Actor
              WHERE UserId = @UserId",
            new { UserId = userId, EmployeeNumber = employeeNumber, Department = department,
                IsActive = isActive, UpdatedAt = updatedAt, Actor = actor },
            transaction,
            cancellationToken: cancellationToken));

        if (updated == 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                @"INSERT INTO StaffMembers
                    (Id, UserId, EmployeeNumber, Department, IsActive, CreatedAt, CreatedBy)
                  VALUES
                    (@Id, @Id, @EmployeeNumber, @Department, @IsActive, @CreatedAt, @Actor)",
                new
                {
                    Id = userId,
                    EmployeeNumber = employeeNumber ?? $"STAFF-{userId:N}",
                    Department = department ?? "Blood Bank",
                    IsActive = isActive,
                    CreatedAt = updatedAt,
                    Actor = actor
                },
                transaction,
                cancellationToken: cancellationToken));
        }
    }

    private (string Select, string[] SearchColumns) GetQuery(MasterDataCategory category)
    {
        var hospitalCode = isSqlite
            ? "COALESCE(NULLIF(HospitalRegNumber, ''), 'HOSP-' || REPLACE(CAST(Id AS TEXT), '-', ''))"
            : "COALESCE(NULLIF(HospitalRegNumber, ''), 'HOSP-' + REPLACE(CONVERT(nvarchar(36), Id), '-', ''))";
        var staffCode = isSqlite
            ? "COALESCE(s.EmployeeNumber, 'STAFF-' || REPLACE(CAST(u.Id AS TEXT), '-', ''))"
            : "COALESCE(s.EmployeeNumber, 'STAFF-' + REPLACE(CONVERT(nvarchar(36), u.Id), '-', ''))";

        return category switch
        {
            MasterDataCategory.Hospital => (
                $@"SELECT Id, {hospitalCode} AS Code, COALESCE(NULLIF(HospitalName, ''), FullName) AS Name,
                          HospitalRegNumber AS RegistrationNumber, FullName AS ContactPerson, Phone, Email,
                          Address, IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                   FROM Users WHERE 1 = 1 AND Role = 'Hospital'",
                ["HospitalName", "FullName", "HospitalRegNumber", "Email", "Phone", "Address"]),
            MasterDataCategory.Doctor => (
                @"SELECT Id, Code, Name, RegistrationNumber, Specialty, HospitalName, Phone, Email,
                         IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                  FROM Doctors WHERE 1 = 1",
                ["Name", "Code", "RegistrationNumber", "Specialty", "HospitalName", "Email", "Phone"]),
            MasterDataCategory.Staff => (
                $@"SELECT u.Id, {staffCode} AS Code, u.FullName AS Name, s.Department, u.Role,
                          u.Phone, u.Email, u.IsActive, COALESCE(s.CreatedAt, u.CreatedAt) AS CreatedAt,
                          COALESCE(s.UpdatedAt, u.UpdatedAt) AS UpdatedAt,
                          COALESCE(s.CreatedBy, u.CreatedBy) AS CreatedBy,
                          COALESCE(s.UpdatedBy, u.UpdatedBy) AS UpdatedBy
                   FROM Users u LEFT JOIN StaffMembers s ON s.UserId = u.Id
                   WHERE 1 = 1 AND u.Role IN ('Admin', 'BloodBank', 'TechnicalSupervisor', 'Technician')",
                ["u.FullName", "s.EmployeeNumber", "u.Email", "u.Phone", "u.Role", "s.Department"]),
            MasterDataCategory.Product => (
                @"SELECT Id, Code, Name, Category, ShelfLifeDays, Description,
                         IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                  FROM Products WHERE 1 = 1",
                ["Name", "Code", "Category", "Description"]),
            MasterDataCategory.BagType => (
                @"SELECT Id, Code, Name, VolumeMl, Anticoagulant, Description,
                         IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                  FROM BagTypes WHERE 1 = 1",
                ["Name", "Code", "Anticoagulant", "Description"]),
            MasterDataCategory.Supplier => (
                @"SELECT Id, Code, Name, ContactPerson, Phone, Email, Address,
                         IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                  FROM Suppliers WHERE 1 = 1",
                ["Name", "Code", "ContactPerson", "Phone", "Email", "Address"]),
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
    }

    private static MasterDataEntry? NormalizeEntry(MasterDataCategory category, MasterDataEntry? entry)
    {
        if (entry is null)
            return null;
        if (category == MasterDataCategory.Product)
        {
            entry.Description ??= string.Empty;
        }
        return entry;
    }

    private static async Task OpenAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is DbConnection dbConnection)
            await dbConnection.OpenAsync(cancellationToken);
        else
            connection.Open();
    }
}
