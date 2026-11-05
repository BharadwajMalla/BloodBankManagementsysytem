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

                    IF OBJECT_ID('ComponentCollections') IS NULL
                    BEGIN
                        CREATE TABLE [ComponentCollections] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [ComponentType] nvarchar(50) NOT NULL,
                            [BloodGroup] nvarchar(10) NOT NULL,
                            [BagType] nvarchar(50) NOT NULL,
                            [VolumeMl] decimal(18,2) NOT NULL,
                            [BagBarcode] nvarchar(50) NOT NULL,
                            [CollectionDate] datetime2 NOT NULL,
                            [ExpiryDate] datetime2 NOT NULL,
                            [Status] nvarchar(50) NOT NULL DEFAULT 'Available'
                        );
                        CREATE UNIQUE INDEX [IX_ComponentCollections_BagBarcode] ON [ComponentCollections] ([BagBarcode]);
                    END
                    IF OBJECT_ID('ComponentDiscards') IS NULL
                    BEGIN
                        CREATE TABLE [ComponentDiscards] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [BagBarcode] nvarchar(50) NOT NULL,
                            [Reason] nvarchar(500) NOT NULL,
                            [DiscardedBy] nvarchar(150) NOT NULL,
                            [ApprovalStatus] nvarchar(50) NOT NULL DEFAULT 'Pending',
                            [ApprovedBy] nvarchar(150) NULL,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [ApprovedAt] datetime2 NULL
                        );
                    END
                    IF OBJECT_ID('ComponentReturns') IS NULL
                    BEGIN
                        CREATE TABLE [ComponentReturns] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [BagBarcode] nvarchar(50) NOT NULL,
                            [Reason] nvarchar(500) NOT NULL,
                            [ReturnedBy] nvarchar(150) NOT NULL,
                            [ReturnDate] datetime2 NOT NULL DEFAULT GETUTCDATE()
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

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BloodRequests') AND name = 'PaymentMode')
                    BEGIN
                        ALTER TABLE [BloodRequests] ADD [PaymentMode] nvarchar(20) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BloodRequests') AND name = 'PaymentStatus')
                    BEGIN
                        ALTER TABLE [BloodRequests] ADD [PaymentStatus] nvarchar(20) NOT NULL CONSTRAINT [DF_BloodRequests_PaymentStatus] DEFAULT 'Pending';
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BloodRequests') AND name = 'TransactionId')
                    BEGIN
                        ALTER TABLE [BloodRequests] ADD [TransactionId] nvarchar(100) NULL;
                    END

                    IF OBJECT_ID('DonationRecords') IS NOT NULL
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'WeightKg') ALTER TABLE [DonationRecords] ADD [WeightKg] decimal(18,2) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'PulsePerMinute') ALTER TABLE [DonationRecords] ADD [PulsePerMinute] int NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'Hemoglobin') ALTER TABLE [DonationRecords] ADD [Hemoglobin] decimal(18,2) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'Hematocrit') ALTER TABLE [DonationRecords] ADD [Hematocrit] decimal(18,2) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'PlateletCount') ALTER TABLE [DonationRecords] ADD [PlateletCount] int NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'BloodPressure') ALTER TABLE [DonationRecords] ADD [BloodPressure] nvarchar(30) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'TemperatureCelsius') ALTER TABLE [DonationRecords] ADD [TemperatureCelsius] decimal(18,2) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'ScreeningStaffName') ALTER TABLE [DonationRecords] ADD [ScreeningStaffName] nvarchar(150) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'ScreeningStaffSignature') ALTER TABLE [DonationRecords] ADD [ScreeningStaffSignature] nvarchar(200) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'ScreeningDate') ALTER TABLE [DonationRecords] ADD [ScreeningDate] datetime2 NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'ScreeningTime') ALTER TABLE [DonationRecords] ADD [ScreeningTime] nvarchar(20) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'MedicalExamAccepted') ALTER TABLE [DonationRecords] ADD [MedicalExamAccepted] bit NOT NULL DEFAULT 0;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'DeferralReason') ALTER TABLE [DonationRecords] ADD [DeferralReason] nvarchar(500) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'MedicalOfficerName') ALTER TABLE [DonationRecords] ADD [MedicalOfficerName] nvarchar(150) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'MedicalOfficerSignature') ALTER TABLE [DonationRecords] ADD [MedicalOfficerSignature] nvarchar(200) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'MedicalOfficerDate') ALTER TABLE [DonationRecords] ADD [MedicalOfficerDate] datetime2 NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'DonorNumber') ALTER TABLE [DonationRecords] ADD [DonorNumber] nvarchar(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'BagType') ALTER TABLE [DonationRecords] ADD [BagType] nvarchar(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'SegmentNumber') ALTER TABLE [DonationRecords] ADD [SegmentNumber] nvarchar(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'DonationStartTime') ALTER TABLE [DonationRecords] ADD [DonationStartTime] nvarchar(20) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'DonationEndTime') ALTER TABLE [DonationRecords] ADD [DonationEndTime] nvarchar(20) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'VenipunctureSite') ALTER TABLE [DonationRecords] ADD [VenipunctureSite] nvarchar(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'PhlebotomistName') ALTER TABLE [DonationRecords] ADD [PhlebotomistName] nvarchar(150) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'PhlebotomistSignature') ALTER TABLE [DonationRecords] ADD [PhlebotomistSignature] nvarchar(200) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'VisualInspectionPassed') ALTER TABLE [DonationRecords] ADD [VisualInspectionPassed] bit NOT NULL DEFAULT 0;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = 'DonorConfirmed') ALTER TABLE [DonationRecords] ADD [DonorConfirmed] bit NOT NULL DEFAULT 0;
                        DECLARE @donationBooleanColumns nvarchar(max) = N'FeelsHealthyToday,DonatedRecently,HadDonationDiscomfort,AteRecently,SleptWell,HasInfectionRisk,HasColdFeverOrSinusitis,TookAntibioticsRecently,TookAlcoholRecently,SmokesFrequently,TookMedicationRecently,HadRecentVaccination,HadRecentIllness,HadRecentSurgeryOrTattoo,HadRecentMalariaOrEndemicExposure,HasChronicDisease,IsPregnantOrBreastfeeding,HadRecentAbortion,HasUnsafeSexualExposure,HasHivOrHepatitisRisk,HasPermanentDisqualification';
                        DECLARE @column sysname;
                        DECLARE donation_columns CURSOR LOCAL FAST_FORWARD FOR SELECT value FROM STRING_SPLIT(@donationBooleanColumns, ',');
                        OPEN donation_columns;
                        FETCH NEXT FROM donation_columns INTO @column;
                        WHILE @@FETCH_STATUS = 0
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonationRecords') AND name = @column)
                                EXEC(N'ALTER TABLE [DonationRecords] ADD [' + @column + N'] bit NOT NULL DEFAULT 0');
                            FETCH NEXT FROM donation_columns INTO @column;
                        END
                        CLOSE donation_columns;
                        DEALLOCATE donation_columns;
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

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IsEmailVerified')
                    BEGIN
                        ALTER TABLE [Users] ADD [IsEmailVerified] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'DateOfBirth')
                    BEGIN
                        ALTER TABLE [Users] ADD [DateOfBirth] datetime2 NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Sex')
                    BEGIN
                        ALTER TABLE [Users] ADD [Sex] nvarchar(20) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Address')
                    BEGIN
                        ALTER TABLE [Users] ADD [Address] nvarchar(300) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BloodRequests') AND name = 'AmountPaid')
                    BEGIN
                        ALTER TABLE [BloodRequests] ADD [AmountPaid] decimal(18,2) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailOtps')
                    BEGIN
                        CREATE TABLE [EmailOtps] (
                            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                            [Email] nvarchar(200) NOT NULL,
                            [OtpCode] nvarchar(10) NOT NULL,
                            [Purpose] nvarchar(50) NOT NULL,
                            [ExpiresAt] datetime2 NOT NULL,
                            [IsUsed] bit NOT NULL DEFAULT 0,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE()
                        );
                        CREATE INDEX [IX_EmailOtps_Email_OtpCode_Purpose] ON [EmailOtps] ([Email], [OtpCode], [Purpose]);
                    END
                ");
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ComponentCollections (
                        Id TEXT NOT NULL PRIMARY KEY,
                        ComponentType TEXT NOT NULL,
                        BloodGroup TEXT NOT NULL,
                        BagType TEXT NOT NULL,
                        VolumeMl REAL NOT NULL,
                        BagBarcode TEXT NOT NULL UNIQUE,
                        CollectionDate TEXT NOT NULL,
                        ExpiryDate TEXT NOT NULL,
                        Status TEXT NOT NULL DEFAULT 'Available'
                    );
                    CREATE TABLE IF NOT EXISTS ComponentDiscards (
                        Id TEXT NOT NULL PRIMARY KEY,
                        BagBarcode TEXT NOT NULL,
                        Reason TEXT NOT NULL,
                        DiscardedBy TEXT NOT NULL,
                        ApprovalStatus TEXT NOT NULL DEFAULT 'Pending',
                        ApprovedBy TEXT NULL,
                        CreatedAt TEXT NOT NULL,
                        ApprovedAt TEXT NULL
                    );
                    CREATE TABLE IF NOT EXISTS ComponentReturns (
                        Id TEXT NOT NULL PRIMARY KEY,
                        BagBarcode TEXT NOT NULL,
                        Reason TEXT NOT NULL,
                        ReturnedBy TEXT NOT NULL,
                        ReturnDate TEXT NOT NULL
                    );");
            }

            // Ensure at least one default Blood Bank admin exists without deleting operational history.
            if (!await context.Users.AnyAsync())
            {
                logger.LogInformation("Creating default Blood Bank A operator account...");
                await context.Users.AddAsync(new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Blood Bank A Administrator",
                    Email = "admin@bloodbankA.org",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "BloodBank",
                    Phone = "+1-800-BLOOD-A",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Database schema synchronized and existing operational history preserved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DbInitializer encountered error: {Message}", ex.Message);
        }
    }

    public static async Task SeedLiveDataAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        // 1. Clear existing operational records
        context.BloodRequests.RemoveRange(context.BloodRequests);
        context.DonationRecords.RemoveRange(context.DonationRecords);
        context.DonationCamps.RemoveRange(context.DonationCamps);
        context.BloodBags.RemoveRange(context.BloodBags);
        context.BloodStocks.RemoveRange(context.BloodStocks);
        context.BloodInventories.RemoveRange(context.BloodInventories);
        context.ComponentCollections.RemoveRange(context.ComponentCollections);
        context.ComponentDiscards.RemoveRange(context.ComponentDiscards);
        context.ComponentReturns.RemoveRange(context.ComponentReturns);

        var existingDonors = await context.Users.Where(u => u.Role == "Donor").ToListAsync();
        context.Users.RemoveRange(existingDonors);

        await context.SaveChangesAsync();

        // 2. Ensure Admin account exists
        if (!await context.Users.AnyAsync(u => u.Email == "admin@bloodbankA.org"))
        {
            await context.Users.AddAsync(new User
            {
                Id = Guid.NewGuid(),
                FullName = "Blood Bank A Administrator",
                Email = "admin@bloodbankA.org",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "BloodBank",
                Phone = "+91-9820011001",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // 3. Seed Registered Indian Donors
        var donorAarav = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Aarav Sharma",
            Email = "aarav.sharma91@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sharma@1991", 11),
            Role = "Donor",
            Phone = "+91-9820145672",
            DateOfBirth = new DateTime(1991, 5, 14, 0, 0, 0, DateTimeKind.Utc),
            Sex = "Male",
            Address = "Flat 402, Sea Green Apts, Worli, Mumbai",
            BloodGroup = "O+",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-10)
        };

        var donorPooja = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Pooja Reddy",
            Email = "pooja.reddy24@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Reddy@1995", 11),
            Role = "Donor",
            Phone = "+91-9849012345",
            DateOfBirth = new DateTime(1995, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            Sex = "Female",
            Address = "Plot 88, Jubilee Hills, Hyderabad",
            BloodGroup = "B+",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        var donorVikram = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Vikram Malhotra",
            Email = "vikram.m88@outlook.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Malhotra@1988", 11),
            Role = "Donor",
            Phone = "+91-9811234567",
            DateOfBirth = new DateTime(1988, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            Sex = "Male",
            Address = "B-12, Greater Kailash Part 1, New Delhi",
            BloodGroup = "A+",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-14)
        };

        var donorAnanya = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Ananya Sen",
            Email = "ananya.sen@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sen@1997", 11),
            Role = "Donor",
            Phone = "+91-9830987654",
            DateOfBirth = new DateTime(1997, 3, 29, 0, 0, 0, DateTimeKind.Utc),
            Sex = "Female",
            Address = "24/B Southern Avenue, Ballygunge, Kolkata",
            BloodGroup = "AB+",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        };

        var donorKarthik = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Karthik Nair",
            Email = "karthik.nair95@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Nair@1993", 11),
            Role = "Donor",
            Phone = "+91-9447123456",
            DateOfBirth = new DateTime(1993, 12, 5, 0, 0, 0, DateTimeKind.Utc),
            Sex = "Male",
            Address = "MG Road, Marine Drive, Kochi",
            BloodGroup = "O-",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-18)
        };

        var donorSneha = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Sneha Patel",
            Email = "sneha.patel@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patel@1996", 11),
            Role = "Donor",
            Phone = "+91-9825123456",
            DateOfBirth = new DateTime(1996, 7, 18, 0, 0, 0, DateTimeKind.Utc),
            Sex = "Female",
            Address = "Sector 21, Gandhinagar, Gujarat",
            BloodGroup = "A-",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-8)
        };

        await context.Users.AddRangeAsync(donorAarav, donorPooja, donorVikram, donorAnanya, donorKarthik, donorSneha);

        // 4. Seed Donation Records for Donors
        await context.DonationRecords.AddRangeAsync(
            new DonationRecord
            {
                Id = Guid.NewGuid(),
                DonorId = donorAarav.Id,
                DonorName = donorAarav.FullName,
                BloodGroup = donorAarav.BloodGroup!,
                UnitsDonated = 1,
                DonationDate = DateTime.UtcNow.AddMonths(-2),
                CampName = "Rotary Club Blood Drive",
                BloodBankName = "Blood Bank A",
                WeightKg = 74.0m,
                PulsePerMinute = 72,
                Hemoglobin = 15.2m,
                Hematocrit = 45.0m,
                BloodPressure = "120/80",
                TemperatureCelsius = 36.6m,
                ScreeningStaffName = "Dr. Radhika Sen",
                MedicalExamAccepted = true,
                VisualInspectionPassed = true,
                DonorConfirmed = true
            },
            new DonationRecord
            {
                Id = Guid.NewGuid(),
                DonorId = donorPooja.Id,
                DonorName = donorPooja.FullName,
                BloodGroup = donorPooja.BloodGroup!,
                UnitsDonated = 1,
                DonationDate = DateTime.UtcNow.AddMonths(-1),
                CampName = "Apollo Health Camp",
                BloodBankName = "Blood Bank A",
                WeightKg = 58.5m,
                PulsePerMinute = 76,
                Hemoglobin = 13.5m,
                Hematocrit = 40.2m,
                BloodPressure = "118/76",
                TemperatureCelsius = 36.5m,
                ScreeningStaffName = "Dr. Radhika Sen",
                MedicalExamAccepted = true,
                VisualInspectionPassed = true,
                DonorConfirmed = true
            },
            new DonationRecord
            {
                Id = Guid.NewGuid(),
                DonorId = donorVikram.Id,
                DonorName = donorVikram.FullName,
                BloodGroup = donorVikram.BloodGroup!,
                UnitsDonated = 1,
                DonationDate = DateTime.UtcNow.AddMonths(-3),
                CampName = "Tech Park Drive",
                BloodBankName = "Blood Bank A",
                WeightKg = 81.0m,
                PulsePerMinute = 70,
                Hemoglobin = 15.8m,
                Hematocrit = 46.5m,
                BloodPressure = "124/82",
                TemperatureCelsius = 36.7m,
                ScreeningStaffName = "Dr. Radhika Sen",
                MedicalExamAccepted = true,
                VisualInspectionPassed = true,
                DonorConfirmed = true
            },
            new DonationRecord
            {
                Id = Guid.NewGuid(),
                DonorId = donorKarthik.Id,
                DonorName = donorKarthik.FullName,
                BloodGroup = donorKarthik.BloodGroup!,
                UnitsDonated = 1,
                DonationDate = DateTime.UtcNow.AddMonths(-4),
                CampName = "Kerala Police Blood Camp",
                BloodBankName = "Blood Bank A",
                WeightKg = 69.0m,
                PulsePerMinute = 74,
                Hemoglobin = 14.6m,
                Hematocrit = 43.8m,
                BloodPressure = "122/78",
                TemperatureCelsius = 36.6m,
                ScreeningStaffName = "Dr. Radhika Sen",
                MedicalExamAccepted = true,
                VisualInspectionPassed = true,
                DonorConfirmed = true
            }
        );

        // 5. Seed Blood Stocks & Bags
        var stockItems = new[]
        {
            ("Whole Blood", "A+", 18),
            ("Whole Blood", "B+", 22),
            ("Whole Blood", "O+", 30),
            ("Whole Blood", "O-", 6),
            ("Whole Blood", "A-", 4),
            ("Whole Blood", "B-", 5),
            ("Whole Blood", "AB+", 8),
            ("Whole Blood", "AB-", 1), // Low stock alert! (1 unit)

            ("Red Blood Cells (PRBC)", "A+", 14),
            ("Red Blood Cells (PRBC)", "B+", 16),
            ("Red Blood Cells (PRBC)", "O+", 25),
            ("Red Blood Cells (PRBC)", "O-", 5),
            ("Red Blood Cells (PRBC)", "AB+", 6),
            ("Red Blood Cells (PRBC)", "AB-", 0), // Depleted stock alert! (0 units)

            ("Platelets", "A+", 10),
            ("Platelets", "B+", 12),
            ("Platelets", "O+", 15),
            ("Platelets", "AB+", 4),

            ("Fresh Frozen Plasma (FFP)", "A+", 15),
            ("Fresh Frozen Plasma (FFP)", "B+", 18),
            ("Fresh Frozen Plasma (FFP)", "O+", 20),
            ("Fresh Frozen Plasma (FFP)", "AB+", 10),

            ("Cryoprecipitate", "A+", 8),
            ("Cryoprecipitate", "O+", 12)
        };

        var bagCounter = 100;
        foreach (var (comp, group, qty) in stockItems)
        {
            await context.BloodStocks.AddAsync(new BloodStock
            {
                Id = Guid.NewGuid(),
                ComponentType = comp,
                BloodGroup = group,
                QuantityUnits = qty,
                BloodBankName = "Blood Bank A",
                LastUpdated = DateTime.UtcNow
            });

            for (int i = 0; i < Math.Min(qty, 2); i++)
            {
                bagCounter++;
                var prefix = comp.Contains("Whole") ? "WB" : comp.Contains("PRBC") ? "PRBC" : comp.Contains("Platelet") ? "PLT" : comp.Contains("Plasma") ? "FFP" : "CRYO";
                var barcode = $"BAG-{prefix}-{group.Replace("+", "P").Replace("-", "N")}-{bagCounter}";
                await context.BloodBags.AddAsync(new BloodBag
                {
                    Id = Guid.NewGuid(),
                    BagNumber = barcode,
                    BloodGroup = group,
                    ComponentType = comp,
                    QuantityUnits = 1,
                    AddedDate = DateTime.UtcNow.AddDays(-i * 2),
                    IsIssued = false
                });

                await context.ComponentCollections.AddAsync(new ComponentCollection
                {
                    Id = Guid.NewGuid(),
                    BagBarcode = barcode,
                    ComponentType = comp,
                    BloodGroup = group,
                    BagType = comp.Contains("Platelet") ? "Single Bag (250ml)" : comp.Contains("Whole") ? "Triple Bag (450ml)" : "Double Bag (350ml)",
                    VolumeMl = comp.Contains("Platelet") ? 250 : comp.Contains("Whole") ? 450 : 350,
                    CollectionDate = DateTime.UtcNow.AddDays(-i * 2),
                    ExpiryDate = DateTime.UtcNow.AddDays(comp.Contains("Platelet") ? 5 : comp.Contains("Whole") ? 35 : 42),
                    Status = "Available"
                });
            }
        }

        // 6. Seed Donation Camps
        await context.DonationCamps.AddRangeAsync(
            new DonationCamp
            {
                Id = Guid.NewGuid(),
                CampName = "Rotary Club Mega Blood Drive 2026",
                Location = "City Community Center, Sector 14",
                CampDate = DateTime.UtcNow.AddDays(5),
                TargetUnits = 100,
                CollectedUnits = 0,
                OrganizedBy = "Rotary Club & Blood Bank A",
                ContactPhone = "+91-9820011223",
                Status = "Scheduled",
                Notes = "Annual corporate & community blood donation drive. Refreshments provided."
            },
            new DonationCamp
            {
                Id = Guid.NewGuid(),
                CampName = "Tech Park Corporate Donation Camp",
                Location = "Cyber Gateway Campus, Building 3",
                CampDate = DateTime.UtcNow.AddDays(12),
                TargetUnits = 75,
                CollectedUnits = 0,
                OrganizedBy = "Blood Bank A",
                ContactPhone = "+91-9884022334",
                Status = "Scheduled",
                Notes = "IT corridor wellness week donation drive."
            },
            new DonationCamp
            {
                Id = Guid.NewGuid(),
                CampName = "Apollo Hospital Public Health Camp",
                Location = "Apollo Hospital Auditorium",
                CampDate = DateTime.UtcNow.AddDays(-14),
                TargetUnits = 60,
                CollectedUnits = 64,
                OrganizedBy = "Apollo & Blood Bank A",
                ContactPhone = "+91-9811234567",
                Status = "Completed",
                Notes = "Exceeded target with 64 units safely collected."
            }
        );

        // 7. Seed Incoming & Fulfilled Blood Requests (with Account Summary & Payments)
        await context.BloodRequests.AddRangeAsync(
            // Pending 1: Critical Emergency Request
            new BloodRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = Guid.NewGuid(),
                RequesterType = "Hospital",
                RequesterName = "Apollo Multi-Specialty Hospital",
                HospitalName = "Apollo Multi-Specialty Hospital",
                RequesterContact = "+91-9840123456",
                PatientName = "Rajesh Verma",
                BloodGroup = "O-",
                ComponentType = "Red Blood Cells (PRBC)",
                QuantityUnits = 2,
                Urgency = "Critical",
                Status = "Pending",
                HospitalDepartment = "Emergency Trauma ICU",
                MedicalReason = "Severe blood loss secondary to road traffic accident surgery",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                PaymentStatus = "Pending"
            },
            // Pending 2: Urgent Request
            new BloodRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = Guid.NewGuid(),
                RequesterType = "Hospital",
                RequesterName = "Care Multi-Speciality Hospital",
                HospitalName = "Care Multi-Speciality Hospital",
                RequesterContact = "+91-9884011223",
                PatientName = "Deepak Kulkarni",
                BloodGroup = "A+",
                ComponentType = "Platelets",
                QuantityUnits = 1,
                Urgency = "Urgent",
                Status = "Pending",
                HospitalDepartment = "Oncology Day Care",
                MedicalReason = "Thrombocytopenia platelet drop during chemotherapy cycle",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                PaymentStatus = "Pending"
            },
            // Fulfilled 1: UPI Paid Request with Transaction ID
            new BloodRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = Guid.NewGuid(),
                RequesterType = "Hospital",
                RequesterName = "Fortis Memorial Hospital",
                HospitalName = "Fortis Memorial Hospital",
                RequesterContact = "+91-9811987654",
                PatientName = "Sunita Deshmukh",
                BloodGroup = "B+",
                ComponentType = "Whole Blood",
                QuantityUnits = 2,
                Urgency = "Normal",
                Status = "Fulfilled",
                HospitalDepartment = "General Surgery",
                MedicalReason = "Elective orthopedic hip replacement surgery",
                IssuedBagNumbers = "BAG-WB-BP-101, BAG-WB-BP-102",
                PaymentMode = "UPI",
                PaymentStatus = "Paid",
                TransactionId = "UPI202611059871",
                AmountPaid = 1700.00m,
                AdminComments = "Cross-match non-reactive. Payment verified via UPI UTR.",
                CreatedAt = DateTime.UtcNow.AddHours(-8),
                ProcessedAt = DateTime.UtcNow.AddHours(-6)
            },
            // Fulfilled 2: Cash Paid Request
            new BloodRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = Guid.NewGuid(),
                RequesterType = "Hospital",
                RequesterName = "City Care Hospital",
                HospitalName = "City Care Hospital",
                RequesterContact = "+91-9820554433",
                PatientName = "Meera Iyer",
                BloodGroup = "O+",
                ComponentType = "Fresh Frozen Plasma (FFP)",
                QuantityUnits = 1,
                Urgency = "Normal",
                Status = "Fulfilled",
                HospitalDepartment = "Hematology",
                MedicalReason = "Pre-operative coagulopathy plasma support",
                IssuedBagNumbers = "BAG-FFP-OP-105",
                PaymentMode = "Cash",
                PaymentStatus = "Paid",
                AmountPaid = 650.00m,
                AdminComments = "Thawed at 37C and issued. Cash received at desk.",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ProcessedAt = DateTime.UtcNow.AddDays(-1).AddHours(2)
            }
        );

        // 8. Seed Component Collections (including near-expiry alert)
        await context.ComponentCollections.AddRangeAsync(
            new ComponentCollection
            {
                Id = Guid.NewGuid(),
                BagBarcode = "BAG-2026-WB-8801",
                ComponentType = "Whole Blood",
                BloodGroup = "O+",
                BagType = "Triple (450ml)",
                VolumeMl = 450,
                CollectionDate = DateTime.UtcNow.AddDays(-5),
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                Status = "Available"
            },
            new ComponentCollection
            {
                Id = Guid.NewGuid(),
                BagBarcode = "BAG-2026-PLT-8802",
                ComponentType = "Platelets",
                BloodGroup = "A+",
                BagType = "Single (250ml)",
                VolumeMl = 250,
                CollectionDate = DateTime.UtcNow.AddDays(-2),
                ExpiryDate = DateTime.UtcNow.AddDays(3), // Expiring in 3 days!
                Status = "Available"
            },
            new ComponentCollection
            {
                Id = Guid.NewGuid(),
                BagBarcode = "BAG-2026-PRBC-8803",
                ComponentType = "Red Blood Cells (PRBC)",
                BloodGroup = "B+",
                BagType = "Quadruple (350ml)",
                VolumeMl = 320,
                CollectionDate = DateTime.UtcNow.AddDays(-8),
                ExpiryDate = DateTime.UtcNow.AddDays(34),
                Status = "Available"
            },
            new ComponentCollection
            {
                Id = Guid.NewGuid(),
                BagBarcode = "BAG-2026-FFP-8804",
                ComponentType = "Fresh Frozen Plasma (FFP)",
                BloodGroup = "AB+",
                BagType = "Double (200ml)",
                VolumeMl = 200,
                CollectionDate = DateTime.UtcNow.AddDays(-10),
                ExpiryDate = DateTime.UtcNow.AddDays(355),
                Status = "Available"
            }
        );

        // 9. Seed Component Discards (including pending approval alert)
        await context.ComponentDiscards.AddRangeAsync(
            new ComponentDiscard
            {
                Id = Guid.NewGuid(),
                BagBarcode = "BAG-2026-DIS-9901",
                Reason = "Gross hemolysis detected during post-centrifugation inspection",
                DiscardedBy = "Lab Tech S. Murthy",
                ApprovalStatus = "Pending",
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            },
            new ComponentDiscard
            {
                Id = Guid.NewGuid(),
                BagBarcode = "BAG-2026-DIS-9902",
                Reason = "Clot formation observed in platelet concentrate bag",
                DiscardedBy = "Lab Tech S. Murthy",
                ApprovalStatus = "Approved",
                ApprovedBy = "Blood Bank Admin",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ApprovedAt = DateTime.UtcNow.AddDays(-2).AddHours(3)
            }
        );

        // 10. Seed Component Returns
        await context.ComponentReturns.AddAsync(
            new ComponentReturn
            {
                Id = Guid.NewGuid(),
                BagBarcode = "BAG-2026-RET-7701",
                Reason = "Surgery postponed by surgical team, cold chain strictly maintained at 4C",
                ReturnedBy = "Apollo Hospital Logistics - Nurse Priya",
                ReturnDate = DateTime.UtcNow.AddHours(-5)
            }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Live example operational data successfully populated directly into the database.");
    }

}
