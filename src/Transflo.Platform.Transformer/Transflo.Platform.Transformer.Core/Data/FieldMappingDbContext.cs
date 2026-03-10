using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Data;

public class FieldMappingDbContext : DbContext
{
    public FieldMappingDbContext(DbContextOptions<FieldMappingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<TmsSystem> TmsSystems { get; set; } = null!;
    public DbSet<Partner> Partners { get; set; } = null!;
    public DbSet<Template> Templates { get; set; } = null!;
    public DbSet<TemplateVersion> TemplateVersions { get; set; } = null!;
    public DbSet<TemplateAssignment> TemplateAssignments { get; set; } = null!;
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
                .WithMany(t => t.FieldMappingTemplates)
                .HasForeignKey(e => e.TmsSystemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.FieldMappingTemplates)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Explicitly ignore the FieldMappings navigation property
            // (TemplateId is a business key, not the FK to Id primary key)
            entity.Ignore(e => e.FieldMappings);
        });

        // Partner configuration
        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });

        // Template configuration
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // TemplateVersion configuration
        modelBuilder.Entity<TemplateVersion>(entity =>
        {
            entity.HasIndex(e => e.TemplateId);
            entity.HasOne(e => e.Template)
                .WithMany(t => t.TemplateVersions)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TemplateAssignment configuration
        modelBuilder.Entity<TemplateAssignment>(entity =>
        {
            entity.HasIndex(e => e.TemplateVersionId);
            entity.HasIndex(e => e.SourcePartnerId);
            entity.HasIndex(e => e.TargetPartnerId);

            entity.HasOne(e => e.TemplateVersion)
                .WithMany(v => v.TemplateAssignments)
                .HasForeignKey(e => e.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SourcePartner)
                .WithMany()
                .HasForeignKey(e => e.SourcePartnerId);

            entity.HasOne(e => e.TargetPartner)
                .WithMany()
                .HasForeignKey(e => e.TargetPartnerId);
        });

        // FieldMapping configuration
        modelBuilder.Entity<FieldMapping>(entity =>
        {
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.TemplateVersionId);
            entity.HasIndex(e => new { e.TemplateId, e.ExecutionOrder });

            // Configure enum to string conversion
            entity.Property(e => e.TransformationType)
                .HasConversion<string>();

            // Explicitly ignore the Template navigation property
            entity.Ignore(e => e.Template);

            entity.HasOne<TemplateVersion>()
                .WithMany(v => v.FieldMappings)
                .HasForeignKey(e => e.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LookupTable configuration
        modelBuilder.Entity<LookupTable>(entity =>
        {
            entity.HasIndex(e => e.TmsSystemId);
            entity.HasIndex(e => e.PartnerId);
            entity.HasIndex(e => new { e.TmsSystemId, e.FieldName });

            entity.HasOne(e => e.TmsSystem)
                .WithMany(t => t.LookupTables)
                .HasForeignKey(e => e.TmsSystemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Partner)
                .WithMany(p => p.LookupTables)
                .HasForeignKey(e => e.PartnerId)
                .OnDelete(DeleteBehavior.SetNull);
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
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
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
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added
                || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.Revision = 1;
            }
            else
            {
                // Increment revision on update
                entity.Revision++;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added
                || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.Revision = 1;
            }
            else
            {
                entity.Revision++;
            }
        }

        return base.SaveChanges();
    }
}
