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
}
