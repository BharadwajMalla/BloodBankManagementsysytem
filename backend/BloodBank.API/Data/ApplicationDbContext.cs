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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // BloodStock Configuration (Blood Bank A)
        modelBuilder.Entity<BloodStock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ComponentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BloodBankName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => new { e.BloodGroup, e.ComponentType, e.BloodBankName }).IsUnique();
        });

        modelBuilder.Entity<BloodBag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BagNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ComponentType).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.BagNumber).IsUnique();
            entity.Property(e => e.AddedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => new { e.BloodGroup, e.ComponentType, e.IsIssued, e.AddedDate });
        });

        // BloodInventory Configuration (Legacy/Standard)
        modelBuilder.Entity<BloodInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.BloodBankName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
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
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.HospitalName).HasMaxLength(150);
            entity.Property(e => e.IssuedBagNumbers).HasMaxLength(2000);
        });

        // DonationRecord Configuration
        modelBuilder.Entity<DonationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DonorName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
            entity.Property(e => e.DonationDate).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
