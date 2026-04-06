using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Data;

public class FieldMappingDbContext : DbContext
{
    public FieldMappingDbContext(DbContextOptions<FieldMappingDbContext> options)
        : base(options)
    {
    }

    public DbSet<TmsSystem> TmsSystems { get; set; } = null!;
    public DbSet<Partner> Partners { get; set; } = null!;
    public DbSet<Template> Templates { get; set; } = null!;
    public DbSet<TemplateVersion> TemplateVersions { get; set; } = null!;
    public DbSet<ApiClient> ApiClients { get; set; } = null!;
    public DbSet<ApiClientTemplateVersion> ApiClientTemplateVersions { get; set; } = null!;
    public DbSet<FieldMapping> FieldMappings { get; set; } = null!;
    public DbSet<LookupTable> LookupTables { get; set; } = null!;
    public DbSet<TransformationLog> TransformationLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TmsSystem configuration
        modelBuilder.Entity<TmsSystem>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
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
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TemplateId, e.Version }).IsUnique();

            // Store enum as string
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Template)
                .WithMany(t => t.TemplateVersions)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiClient configuration
        modelBuilder.Entity<ApiClient>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
        });

        // ApiClientTemplateVersion configuration
        modelBuilder.Entity<ApiClientTemplateVersion>(entity =>
        {
            entity.HasIndex(e => e.ApiClientId);
            entity.HasIndex(e => e.TemplateVersionId);
            entity.HasIndex(e => new { e.ApiClientId, e.TemplateVersionId }).IsUnique();

            entity.HasOne(e => e.ApiClient)
                .WithMany(c => c.ApiClientTemplateVersions)
                .HasForeignKey(e => e.ApiClientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TemplateVersion)
                .WithMany(v => v.ApiClientTemplateVersions)
                .HasForeignKey(e => e.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FieldMapping configuration
        modelBuilder.Entity<FieldMapping>(entity =>
        {
            entity.HasIndex(e => e.TemplateVersionId);
            entity.HasIndex(e => new { e.TemplateVersionId, e.ExecutionOrder });

            // Configure enum to string conversion
            entity.Property(e => e.TransformationType)
                .HasConversion<string>();

            entity.HasOne(e => e.TemplateVersion)
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
            entity.HasIndex(e => e.CorrelationId);

            // Configure enum to string conversion
            entity.Property(e => e.Status)
                .HasConversion<string>();
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<TmsSystem>().HasData(
            new TmsSystem
            {
                Id = Guid.Parse("b5f3a9c2-7d4e-4f8b-9a1c-3e6d2b0f8c47"),
                Name = "TruckMate",
                DisplayName = "TruckMate TMS",
                Description = "TruckMate Transportation Management System",
                Version = "1.0",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                CreatedBy = "System"
            },
            new TmsSystem
            {
                Id = Guid.Parse("a2c8e4d6-1f3b-4a7c-8e9d-5b0c2f6a4e83"),
                Name = "McLeod",
                DisplayName = "McLeod Software",
                Description = "McLeod Transportation Management System",
                Version = "1.0",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
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
