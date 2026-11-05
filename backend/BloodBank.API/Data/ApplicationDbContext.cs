using Microsoft.EntityFrameworkCore;
using BloodBank.API.Models;

namespace BloodBank.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<BloodStock> BloodStocks => Set<BloodStock>();
    public DbSet<BloodBag> BloodBags => Set<BloodBag>();
    public DbSet<BloodInventory> BloodInventories => Set<BloodInventory>();
    public DbSet<DonationCamp> DonationCamps => Set<DonationCamp>();
    public DbSet<BloodRequest> BloodRequests => Set<BloodRequest>();
    public DbSet<DonationRecord> DonationRecords => Set<DonationRecord>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
    public DbSet<ComponentCollection> ComponentCollections => Set<ComponentCollection>();
    public DbSet<ComponentDiscard> ComponentDiscards => Set<ComponentDiscard>();
    public DbSet<ComponentReturn> ComponentReturns => Set<ComponentReturn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var isSqlite = Database.IsSqlite();
        var utcNowSql = isSqlite ? "CURRENT_TIMESTAMP" : "GETUTCDATE()";

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Sex).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNowSql);
        });

        // BloodStock Configuration (Blood Bank A)
        modelBuilder.Entity<BloodStock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ComponentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BloodBankName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql(utcNowSql);
            entity.HasIndex(e => new { e.BloodGroup, e.ComponentType, e.BloodBankName }).IsUnique();
        });

        modelBuilder.Entity<BloodBag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BagNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ComponentType).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.BagNumber).IsUnique();
            entity.Property(e => e.AddedDate).HasDefaultValueSql(utcNowSql);
            entity.HasIndex(e => new { e.BloodGroup, e.ComponentType, e.IsIssued, e.AddedDate });
        });

        // BloodInventory Configuration (Legacy/Standard)
        modelBuilder.Entity<BloodInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.BloodBankName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql(utcNowSql);
        });

        // DonationCamp Configuration
        modelBuilder.Entity<DonationCamp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CampName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("Scheduled");
        });

        // BloodRequest Configuration
        modelBuilder.Entity<BloodRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequesterType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PatientName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ComponentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNowSql);
            entity.Property(e => e.HospitalName).HasMaxLength(150);
            entity.Property(e => e.IssuedBagNumbers).HasMaxLength(2000);
            entity.Property(e => e.PaymentMode).HasMaxLength(20);
            entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.TransactionId).HasMaxLength(100);
        });

        modelBuilder.Entity<ComponentCollection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BagBarcode).IsUnique();
            entity.Property(e => e.ComponentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.BagType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BagBarcode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.VolumeMl).HasPrecision(18, 2);
        });
        modelBuilder.Entity<ComponentDiscard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BagBarcode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DiscardedBy).IsRequired().HasMaxLength(150);
            entity.Property(e => e.ApprovalStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ApprovedBy).HasMaxLength(150);
        });
        modelBuilder.Entity<ComponentReturn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BagBarcode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ReturnedBy).IsRequired().HasMaxLength(150);
        });

        // DonationRecord Configuration
        modelBuilder.Entity<DonationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DonorName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.DonationDate).HasDefaultValueSql(utcNowSql);
            entity.Property(e => e.BloodPressure).HasMaxLength(30);
            entity.Property(e => e.ScreeningStaffName).HasMaxLength(150);
            entity.Property(e => e.ScreeningStaffSignature).HasMaxLength(200);
            entity.Property(e => e.DeferralReason).HasMaxLength(500);
            entity.Property(e => e.MedicalOfficerName).HasMaxLength(150);
            entity.Property(e => e.MedicalOfficerSignature).HasMaxLength(200);
            entity.Property(e => e.DonorNumber).HasMaxLength(100);
            entity.Property(e => e.BagType).HasMaxLength(50);
            entity.Property(e => e.SegmentNumber).HasMaxLength(100);
            entity.Property(e => e.DonationStartTime).HasMaxLength(20);
            entity.Property(e => e.DonationEndTime).HasMaxLength(20);
            entity.Property(e => e.VenipunctureSite).HasMaxLength(50);
            entity.Property(e => e.PhlebotomistName).HasMaxLength(150);
            entity.Property(e => e.PhlebotomistSignature).HasMaxLength(200);
        });

        // EmailOtp Configuration
        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Purpose).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNowSql);
            entity.HasIndex(e => new { e.Email, e.OtpCode, e.Purpose });
        });
    }
}
