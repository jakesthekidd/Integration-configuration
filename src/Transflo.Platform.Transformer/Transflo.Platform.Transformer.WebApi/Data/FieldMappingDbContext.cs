using Microsoft.EntityFrameworkCore;
using FieldMappingApi.Models;

namespace FieldMappingApi.Data;

public class FieldMappingDbContext : DbContext
{
    public FieldMappingDbContext(DbContextOptions<FieldMappingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<TmsSystem> TmsSystems { get; set; } = null!;
    public DbSet<FieldMappingTemplate> FieldMappingTemplates { get; set; } = null!;
    public DbSet<FieldMapping> FieldMappings { get; set; } = null!;
    public DbSet<LookupTable> LookupTables { get; set; } = null!;
    public DbSet<TransformationLog> TransformationLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("code IS NOT NULL");
            entity.HasIndex(e => e.IsActive);
        });

        // TmsSystem configuration
        modelBuilder.Entity<TmsSystem>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // FieldMappingTemplate configuration
        modelBuilder.Entity<FieldMappingTemplate>(entity =>
        {
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => new { e.TemplateId, e.Version }).IsUnique();
            entity.HasIndex(e => e.TmsSystemId);
            entity.HasIndex(e => e.Status);

            // Configure enum to string conversion
            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.HasOne(e => e.TmsSystem)
                .WithMany(t => t.Templates)
                .HasForeignKey(e => e.TmsSystemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Templates)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Explicitly ignore the FieldMappings navigation property
            // (TemplateId is a business key, not the FK to Id primary key)
            entity.Ignore(e => e.FieldMappings);
        });

        // FieldMapping configuration
        modelBuilder.Entity<FieldMapping>(entity =>
        {
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => new { e.TemplateId, e.ExecutionOrder });

            // Configure enum to string conversion
            entity.Property(e => e.TransformationType)
                .HasConversion<string>();

            // Explicitly ignore the Template navigation property
            entity.Ignore(e => e.Template);
        });

        // LookupTable configuration
        modelBuilder.Entity<LookupTable>(entity =>
        {
            entity.HasIndex(e => e.TmsSystemId);
            entity.HasIndex(e => new { e.TmsSystemId, e.FieldName });

            entity.HasOne(e => e.TmsSystem)
                .WithMany(t => t.LookupTables)
                .HasForeignKey(e => e.TmsSystemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TransformationLog configuration
        modelBuilder.Entity<TransformationLog>(entity =>
        {
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ExpiresAt);

            // Configure enum to string conversion
            entity.Property(e => e.Status)
                .HasConversion<string>();
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        // Seed TMS Systems
        modelBuilder.Entity<TmsSystem>().HasData(
            new TmsSystem
            {
                Id = "tms-truckmate-001",
                Name = "TruckMate",
                DisplayName = "TruckMate TMS",
                Description = "TruckMate Transportation Management System",
                Version = "1.0",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            },
            new TmsSystem
            {
                Id = "tms-mcleod-001",
                Name = "McLeod",
                DisplayName = "McLeod Software",
                Description = "McLeod Transportation Management System",
                Version = "1.0",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            }
        );
    }
}
