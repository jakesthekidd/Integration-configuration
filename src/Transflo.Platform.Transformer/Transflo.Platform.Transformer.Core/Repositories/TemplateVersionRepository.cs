using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.Core.Repositories;

public class TemplateVersionRepository : ITemplateVersionRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<TemplateVersionRepository> _logger;

    public TemplateVersionRepository(FieldMappingDbContext context, ILogger<TemplateVersionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TemplateVersion?> GetByVersionAsync(Guid templateId, int version) =>
        await _context.TemplateVersions
            .FirstOrDefaultAsync(v => v.TemplateId == templateId && v.Version == version);

    public async Task<TemplateVersion?> GetPublishedVersionAsync(Guid templateId) =>
        await _context.TemplateVersions
            .Where(v => v.TemplateId == templateId && v.Status == TemplateVersionStatus.Published)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync();

    public async Task<List<TemplateVersion>> GetAllVersionsAsync(Guid templateId) =>
        await _context.TemplateVersions
            .Where(v => v.TemplateId == templateId)
            .OrderByDescending(v => v.Version)
            .ToListAsync();

    public async Task<bool> HasClientAccessAsync(Guid templateVersionId, Guid clientId)
    {
        return await _context.ApiClientTemplateVersions
             .AnyAsync(actv => actv.TemplateVersionId == templateVersionId && actv.ApiClientId == clientId);
    }
    public async Task<TemplateVersion> CreateAsync(TemplateVersion version)
    {
        _context.TemplateVersions.Add(version);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created template version {Version} for template {TemplateId}",
            version.Version, version.TemplateId);

        return version;
    }

    public async Task<TemplateVersion> UpdateAsync(TemplateVersion version)
    {
        _context.TemplateVersions.Update(version);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated template version {Version} for template {TemplateId} → {Status}",
            version.Version, version.TemplateId, version.Status);

        return version;
    }

    public async Task<TemplateVersion> PublishVersionAsync(
        Guid templateId,
        int version,
        string? publishedBy = null)
    {
        var target = await GetByVersionAsync(templateId, version)
            ?? throw new InvalidOperationException(
                $"Template version {version} not found for template {templateId}.");

        if (target.Status != TemplateVersionStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Only Draft versions can be published. Current status: {target.Status}.");
        }

        // Mark current Published version as Superseded (if any)
        var currentPublished = await GetPublishedVersionAsync(templateId);
        if (currentPublished is not null)
        {
            currentPublished.Status = TemplateVersionStatus.Superseded;
            _context.TemplateVersions.Update(currentPublished);
        }

        // Promote target to Published
        target.Status = TemplateVersionStatus.Published;
        target.PublishedAt = DateTime.UtcNow;
        target.PublishedBy = publishedBy;
        _context.TemplateVersions.Update(target);

        // Single SaveChanges — both updates are atomic
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Published template version {Version} for template {TemplateId}. Previous published v{Previous} → Superseded.",
            version, templateId, currentPublished?.Version);

        return target;
    }

    public async Task<bool> DeleteAsync(Guid templateId, int version)
    {
        var target = await GetByVersionAsync(templateId, version);
        if (target is null)
        {
            return false;
        }

        _context.TemplateVersions.Remove(target);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted template version {Version} for template {TemplateId}",
            version, templateId);

        return true;
    }
}
