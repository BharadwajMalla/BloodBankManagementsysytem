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

            // 3. Clear all operational tables so the user starts with a 100% clean, empty database
            logger.LogInformation("Ensuring clean slate: wiping all operational stocks, bags, camps, requests, and donation history...");

            context.BloodStocks.RemoveRange(context.BloodStocks);
            context.BloodBags.RemoveRange(context.BloodBags);
            context.DonationCamps.RemoveRange(context.DonationCamps);
            context.BloodRequests.RemoveRange(context.BloodRequests);
            context.DonationRecords.RemoveRange(context.DonationRecords);
            context.BloodInventories.RemoveRange(context.BloodInventories);

            // Ensure at least one default Blood Bank admin exists if the Users table was completely wiped
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
            logger.LogInformation("Database operational tables are now completely empty and ready for fresh user testing.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DbInitializer encountered error: {Message}", ex.Message);
        }
    }
}
