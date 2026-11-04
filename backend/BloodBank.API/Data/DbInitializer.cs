using Microsoft.EntityFrameworkCore;
using BloodBank.API.Models;

namespace BloodBank.API.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            logger.LogInformation("Synchronizing Blood Bank A database schema...");

            // 1. Ensure basic schema is migrated (SQL Server uses Migrations & raw DDL, SQLite uses EnsureCreated)
            if (context.Database.IsSqlServer())
            {
                try
                {
                    await context.Database.MigrateAsync();
                }
                catch (Exception migEx)
                {
                    logger.LogWarning("MigrateAsync encountered warning, ensuring schema via idempotent DDL: {Message}", migEx.Message);
                }

                // 2. Execute idempotent DDL to ensure all new Blood Bank A tables and columns exist
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE [Users] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [FullName] nvarchar(150) NOT NULL,
                            [Email] nvarchar(200) NOT NULL,
                            [PasswordHash] nvarchar(max) NOT NULL,
                            [Role] nvarchar(50) NOT NULL,
                            [Phone] nvarchar(20) NULL,
                            [BloodGroup] nvarchar(10) NULL,
                            [HospitalName] nvarchar(150) NULL,
                            [HospitalRegNumber] nvarchar(50) NULL,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE()
                        );
                        CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BloodStocks')
                    BEGIN
                        CREATE TABLE [BloodStocks] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [BloodGroup] nvarchar(10) NOT NULL,
                            [ComponentType] nvarchar(50) NOT NULL,
                            [QuantityUnits] int NOT NULL,
                            [BloodBankName] nvarchar(100) NOT NULL DEFAULT 'Blood Bank A',
                            [LastUpdated] datetime2 NOT NULL DEFAULT GETUTCDATE()
                        );
                        CREATE UNIQUE INDEX [IX_BloodStocks_BloodGroup_ComponentType_BloodBankName] ON [BloodStocks] ([BloodGroup], [ComponentType], [BloodBankName]);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BloodInventories')
                    BEGIN
                        CREATE TABLE [BloodInventories] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [BloodGroup] nvarchar(10) NOT NULL,
                            [QuantityUnits] int NOT NULL DEFAULT 0,
                            [BloodBankName] nvarchar(200) NOT NULL DEFAULT 'Blood Bank A',
                            [LastUpdated] datetime2 NOT NULL DEFAULT GETUTCDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BloodBags')
                    BEGIN
                        CREATE TABLE [BloodBags] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [BagNumber] nvarchar(50) NOT NULL,
                            [BloodGroup] nvarchar(10) NOT NULL,
                            [ComponentType] nvarchar(50) NOT NULL,
                            [QuantityUnits] int NOT NULL,
                            [AddedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [IsIssued] bit NOT NULL DEFAULT 0,
                            [IssuedDate] datetime2 NULL
                        );
                        CREATE UNIQUE INDEX [IX_BloodBags_BagNumber] ON [BloodBags] ([BagNumber]);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DonationCamps')
                    BEGIN
                        CREATE TABLE [DonationCamps] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [CampName] nvarchar(200) NOT NULL,
                            [Location] nvarchar(250) NOT NULL,
                            [CampDate] datetime2 NOT NULL,
                            [TargetUnits] int NOT NULL DEFAULT 50,
                            [CollectedUnits] int NOT NULL DEFAULT 0,
                            [OrganizedBy] nvarchar(100) NOT NULL DEFAULT 'Blood Bank A',
                            [ContactPhone] nvarchar(50) NOT NULL DEFAULT '',
                            [Status] nvarchar(30) NOT NULL DEFAULT 'Scheduled',
                            [Notes] nvarchar(500) NULL,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BloodRequests')
                    BEGIN
                        CREATE TABLE [BloodRequests] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [RequesterId] uniqueidentifier NOT NULL,
                            [RequesterType] nvarchar(50) NOT NULL DEFAULT 'Hospital',
                            [RequesterName] nvarchar(150) NOT NULL,
                            [RequesterContact] nvarchar(50) NULL,
                            [PatientName] nvarchar(150) NOT NULL,
                            [BloodGroup] nvarchar(10) NOT NULL,
                            [ComponentType] nvarchar(50) NOT NULL DEFAULT 'Whole Blood',
                            [QuantityUnits] int NOT NULL DEFAULT 1,
                            [Urgency] nvarchar(20) NOT NULL DEFAULT 'Normal',
                            [Status] nvarchar(30) NOT NULL DEFAULT 'Pending',
                            [HospitalDepartment] nvarchar(100) NULL,
                            [MedicalReason] nvarchar(500) NULL,
                            [AdminComments] nvarchar(500) NULL,
                            [HospitalName] nvarchar(150) NULL,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [ProcessedAt] datetime2 NULL,
                            [IssuedBagNumbers] nvarchar(2000) NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DonationRecords')
                    BEGIN
                        CREATE TABLE [DonationRecords] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [DonorId] uniqueidentifier NOT NULL,
                            [DonorName] nvarchar(150) NOT NULL,
                            [BloodGroup] nvarchar(10) NOT NULL,
                            [UnitsDonated] int NOT NULL DEFAULT 1,
                            [DonationDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [CampName] nvarchar(200) NULL,
                            [BloodBankName] nvarchar(50) NOT NULL DEFAULT 'Blood Bank A'
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Phone')
                    BEGIN
                        ALTER TABLE [Users] ADD [Phone] nvarchar(20) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BloodRequests') AND name = 'HospitalName')
                    BEGIN
                        ALTER TABLE [BloodRequests] ADD [HospitalName] nvarchar(150) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BloodRequests') AND name = 'IssuedBagNumbers')
                    BEGIN
                        ALTER TABLE [BloodRequests] ADD [IssuedBagNumbers] nvarchar(2000) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BloodBags') AND name = 'IsIssued')
                    BEGIN
                        ALTER TABLE [BloodBags] ADD [IsIssued] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BloodBags') AND name = 'IssuedDate')
                    BEGIN
                        ALTER TABLE [BloodBags] ADD [IssuedDate] datetime2 NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'BloodGroup')
                    BEGIN
                        ALTER TABLE [Users] ADD [BloodGroup] nvarchar(10) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'HospitalName')
                    BEGIN
                        ALTER TABLE [Users] ADD [HospitalName] nvarchar(150) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'HospitalRegNumber')
                    BEGIN
                        ALTER TABLE [Users] ADD [HospitalRegNumber] nvarchar(50) NULL;
                    END
                ");
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // 3. Seed Users (BloodBank A, Hospital, Donor)
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@bloodbankA.org");
            if (adminUser == null)
            {
                logger.LogInformation("Seeding Blood Bank A users...");

                var donor1Id = Guid.NewGuid();
                var donor2Id = Guid.NewGuid();
                var hospitalId = Guid.NewGuid();

                var seedUsers = new List<User>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        FullName = "Blood Bank A Administrator",
                        Email = "admin@bloodbankA.org",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("BloodBank@123"),
                        Role = "BloodBank",
                        Phone = "+1-800-BLOOD-A",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = hospitalId,
                        FullName = "Dr. Robert Vance",
                        Email = "cityhospital@hospital.org",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Hospital@123"),
                        Role = "Hospital",
                        HospitalName = "St. Jude City Hospital",
                        HospitalRegNumber = "HOSP-REG-98214",
                        Phone = "+1-555-789-0123",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = donor1Id,
                        FullName = "Johnathan Doe",
                        Email = "john.donor@gmail.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Donor@123"),
                        Role = "Donor",
                        BloodGroup = "O+",
                        Phone = "+1-555-234-5678",
                        CreatedAt = DateTime.UtcNow.AddMonths(-6)
                    },
                    new()
                    {
                        Id = donor2Id,
                        FullName = "Sarah Jenkins",
                        Email = "sarah.jenkins@gmail.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Donor@123"),
                        Role = "Donor",
                        BloodGroup = "A-",
                        Phone = "+1-555-876-5432",
                        CreatedAt = DateTime.UtcNow.AddMonths(-4)
                    }
                };

                await context.Users.AddRangeAsync(seedUsers);
                await context.SaveChangesAsync();

                // Seed historical donation records
                var seedDonations = new List<DonationRecord>
                {
                    new() { Id = Guid.NewGuid(), DonorId = donor1Id, DonorName = "Johnathan Doe", BloodGroup = "O+", UnitsDonated = 1, DonationDate = DateTime.UtcNow.AddMonths(-4), CampName = "City Hall Blood Drive", BloodBankName = "Blood Bank A" },
                    new() { Id = Guid.NewGuid(), DonorId = donor2Id, DonorName = "Sarah Jenkins", BloodGroup = "A-", UnitsDonated = 1, DonationDate = DateTime.UtcNow.AddMonths(-2), CampName = "Metro University Camp", BloodBankName = "Blood Bank A" }
                };
                await context.DonationRecords.AddRangeAsync(seedDonations);
                await context.SaveChangesAsync();
            }

            // 4. Seed Blood Bank A Stock
            if (!await context.BloodStocks.AnyAsync())
            {
                logger.LogInformation("Seeding Blood Bank A stock levels...");

                var bloodGroups = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
                var stocks = new List<BloodStock>();

                foreach (var bg in bloodGroups)
                {
                    // Whole Blood
                    stocks.Add(new BloodStock
                    {
                        Id = Guid.NewGuid(),
                        BloodGroup = bg,
                        ComponentType = "Whole Blood",
                        QuantityUnits = bg == "O-" ? 4 : (bg == "AB-" ? 3 : 25),
                        BloodBankName = "Blood Bank A",
                        LastUpdated = DateTime.UtcNow
                    });

                    // PRBC (Packed Red Blood Cells)
                    stocks.Add(new BloodStock
                    {
                        Id = Guid.NewGuid(),
                        BloodGroup = bg,
                        ComponentType = "Red Blood Cells (PRBC)",
                        QuantityUnits = bg == "O-" ? 2 : 18,
                        BloodBankName = "Blood Bank A",
                        LastUpdated = DateTime.UtcNow
                    });

                    // Platelets
                    stocks.Add(new BloodStock
                    {
                        Id = Guid.NewGuid(),
                        BloodGroup = bg,
                        ComponentType = "Platelets",
                        QuantityUnits = 12,
                        BloodBankName = "Blood Bank A",
                        LastUpdated = DateTime.UtcNow
                    });

                    // Fresh Frozen Plasma (FFP)
                    stocks.Add(new BloodStock
                    {
                        Id = Guid.NewGuid(),
                        BloodGroup = bg,
                        ComponentType = "Fresh Frozen Plasma (FFP)",
                        QuantityUnits = 15,
                        BloodBankName = "Blood Bank A",
                        LastUpdated = DateTime.UtcNow
                    });
                }

                await context.BloodStocks.AddRangeAsync(stocks);
                await context.SaveChangesAsync();
            }

            // 5. Seed Donation Camps for Blood Bank A
            if (!await context.DonationCamps.AnyAsync())
            {
                logger.LogInformation("Seeding Blood Bank A donation camps...");

                var seedCamps = new List<DonationCamp>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        CampName = "Metro Civic Center Mega Blood Drive",
                        Location = "Metro Civic Center, Auditorium Hall B",
                        CampDate = DateTime.UtcNow.AddDays(3),
                        TargetUnits = 100,
                        CollectedUnits = 0,
                        OrganizedBy = "Blood Bank A",
                        ContactPhone = "+1-800-BLOOD-A",
                        Status = "Scheduled",
                        Notes = "Walk-ins welcome. Free health check-up and refreshments provided.",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        CampName = "St. Paul Church Community Donation Camp",
                        Location = "St. Paul Fellowship Hall, 4th Avenue",
                        CampDate = DateTime.UtcNow.AddDays(9),
                        TargetUnits = 60,
                        CollectedUnits = 0,
                        OrganizedBy = "Blood Bank A",
                        ContactPhone = "+1-800-BLOOD-A",
                        Status = "Scheduled",
                        Notes = "Focusing on rare negative blood groups.",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        CampName = "Downtown Tech Park Corporate Donation Camp",
                        Location = "Cyber Towers, 1st Floor Cafeteria",
                        CampDate = DateTime.UtcNow.AddDays(-10),
                        TargetUnits = 80,
                        CollectedUnits = 94,
                        OrganizedBy = "Blood Bank A",
                        Status = "Completed",
                        Notes = "Successfully gathered 94 units.",
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    }
                };

                await context.DonationCamps.AddRangeAsync(seedCamps);
                await context.SaveChangesAsync();
            }

            // 6. Seed Initial Requests for demonstration
            if (!await context.BloodRequests.AnyAsync())
            {
                logger.LogInformation("Seeding initial requests for Blood Bank A...");

                var hospital = await context.Users.FirstOrDefaultAsync(u => u.Role == "Hospital");
                var donor = await context.Users.FirstOrDefaultAsync(u => u.Role == "Donor");

                var seedRequests = new List<BloodRequest>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        RequesterId = hospital?.Id ?? Guid.NewGuid(),
                        RequesterType = "Hospital",
                        RequesterName = hospital?.HospitalName ?? "St. Jude City Hospital",
                        RequesterContact = hospital?.Phone ?? "+1-555-789-0123",
                        PatientName = "Emily Clarke",
                        BloodGroup = "O-",
                        ComponentType = "Red Blood Cells (PRBC)",
                        QuantityUnits = 2,
                        Urgency = "Critical",
                        Status = "Pending",
                        HospitalDepartment = "Trauma Emergency Ward",
                        MedicalReason = "Severe blood loss from road accident surgery",
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        RequesterId = donor?.Id ?? Guid.NewGuid(),
                        RequesterType = "Donor",
                        RequesterName = donor?.FullName ?? "Johnathan Doe",
                        RequesterContact = donor?.Phone ?? "+1-555-234-5678",
                        PatientName = "Margaret Doe (Mother)",
                        BloodGroup = "A+",
                        ComponentType = "Platelets",
                        QuantityUnits = 1,
                        Urgency = "Urgent",
                        Status = "Pending",
                        HospitalDepartment = "Oncology Wing",
                        MedicalReason = "Platelet drop during chemotherapy treatment",
                        CreatedAt = DateTime.UtcNow.AddHours(-5)
                    }
                };

                await context.BloodRequests.AddRangeAsync(seedRequests);
                await context.SaveChangesAsync();
            }

            logger.LogInformation("Blood Bank A database synchronized and seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DbInitializer encountered error: {Message}", ex.Message);
        }
    }
}
