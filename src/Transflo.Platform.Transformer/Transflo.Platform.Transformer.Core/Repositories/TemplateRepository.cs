using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories;

public interface ITemplateRepository
{
    Task<FieldMappingTemplate?> GetByIdAsync(Guid templateId, int? version = null);
    Task<FieldMappingTemplate?> GetLatestVersionAsync(Guid templateId);
    Task<List<FieldMappingTemplate>> GetByTmsSystemIdAsync(Guid tmsSystemId);
    Task<List<FieldMappingTemplate>> GetAllAsync();
    Task<FieldMappingTemplate> CreateAsync(FieldMappingTemplate template);
    Task<FieldMappingTemplate> UpdateAsync(FieldMappingTemplate template);
    Task DeleteAsync(Guid templateId, int? version = null);
}

public class TemplateRepository : ITemplateRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<TemplateRepository> _logger;

    public TemplateRepository(FieldMappingDbContext context, ILogger<TemplateRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FieldMappingTemplate?> GetByIdAsync(Guid templateId, int? version = null)
    {
        if (version.HasValue)
        {
            return await _context.FieldMappingTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.Version == version.Value);
        }

        return await GetLatestVersionAsync(templateId);
    }

    public async Task<FieldMappingTemplate?> GetLatestVersionAsync(Guid templateId)
    {
        return await _context.FieldMappingTemplates
            .Where(t => t.TemplateId == templateId)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<List<FieldMappingTemplate>> GetByTmsSystemIdAsync(Guid tmsSystemId)
    {
        return await _context.FieldMappingTemplates
            .Where(t => t.TmsSystemId == tmsSystemId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FieldMappingTemplate>> GetAllAsync()
    {
        return await _context.FieldMappingTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<FieldMappingTemplate> CreateAsync(FieldMappingTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        _context.FieldMappingTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created template: {template.Name} (ID: {template.TemplateId}, Version: {template.Version})");
        return template;
    }

    public async Task<FieldMappingTemplate> UpdateAsync(FieldMappingTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;

        _context.FieldMappingTemplates.Update(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Updated template: {template.TemplateId}");
        return template;
    }

    public async Task DeleteAsync(Guid templateId, int? version = null)
    {
        var templatesToDelete = version.HasValue
            ? await _context.FieldMappingTemplates.Where(t => t.TemplateId == templateId && t.Version == version.Value).ToListAsync()
            : await _context.FieldMappingTemplates.Where(t => t.TemplateId == templateId).ToListAsync();

        if (templatesToDelete.Any())
        {
            _context.FieldMappingTemplates.RemoveRange(templatesToDelete);
            await _context.SaveChangesAsync();

            if (version.HasValue)
            {
                _logger.LogInformation($"Deleted template: {templateId} v{version}");
            }
            else
            {
                _logger.LogInformation($"Deleted all versions of template: {templateId}");
            }
        }
        else
        {
            var templates = await _context.FieldMappingTemplates
                .Where(t => t.TemplateId == templateId)
                .ToListAsync();

            _context.FieldMappingTemplates.RemoveRange(templates);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted all versions of template: {templateId}");
        }
    }
}
