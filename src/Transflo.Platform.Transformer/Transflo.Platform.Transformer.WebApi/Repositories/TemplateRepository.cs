using Microsoft.EntityFrameworkCore;
using FieldMappingApi.Data;
using FieldMappingApi.Models;

namespace FieldMappingApi.Repositories;

public interface ITemplateRepository
{
    Task<FieldMappingTemplate?> GetByIdAsync(string templateId, int? version = null);
    Task<FieldMappingTemplate?> GetLatestVersionAsync(string templateId);
    Task<List<FieldMappingTemplate>> GetByTmsSystemIdAsync(string tmsSystemId);
    Task<List<FieldMappingTemplate>> GetAllAsync();
    Task<FieldMappingTemplate> CreateAsync(FieldMappingTemplate template);
    Task<FieldMappingTemplate> UpdateAsync(FieldMappingTemplate template);
    Task DeleteAsync(string templateId, int? version = null);
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

    public async Task<FieldMappingTemplate?> GetByIdAsync(string templateId, int? version = null)
    {
        if (version.HasValue)
        {
            return await _context.FieldMappingTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.Version == version.Value);
        }

        return await GetLatestVersionAsync(templateId);
    }

    public async Task<FieldMappingTemplate?> GetLatestVersionAsync(string templateId)
    {
        return await _context.FieldMappingTemplates
            .Where(t => t.TemplateId == templateId)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<List<FieldMappingTemplate>> GetByTmsSystemIdAsync(string tmsSystemId)
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

    public async Task DeleteAsync(string templateId, int? version = null)
    {
        if (version.HasValue)
        {
            var template = await GetByIdAsync(templateId, version);
            if (template != null)
            {
                _context.FieldMappingTemplates.Remove(template);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted template: {templateId} v{version}");
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
