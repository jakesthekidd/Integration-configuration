using Microsoft.EntityFrameworkCore;
using FieldMappingApi.Data;
using FieldMappingApi.Models;

namespace FieldMappingApi.Repositories;

public interface IFieldMappingRepository
{
    Task<FieldMapping?> GetByIdAsync(string id);
    Task<List<FieldMapping>> GetByTemplateIdAsync(string templateId);
    Task<List<FieldMapping>> GetByTemplateIdOrderedAsync(string templateId);
    Task<FieldMapping> CreateAsync(FieldMapping mapping);
    Task<List<FieldMapping>> CreateBulkAsync(List<FieldMapping> mappings);
    Task<FieldMapping> UpdateAsync(FieldMapping mapping);
    Task DeleteAsync(string id);
    Task DeleteByTemplateIdAsync(string templateId);
}

public class FieldMappingRepository : IFieldMappingRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<FieldMappingRepository> _logger;

    public FieldMappingRepository(FieldMappingDbContext context, ILogger<FieldMappingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FieldMapping?> GetByIdAsync(string id)
    {
        return await _context.FieldMappings.FindAsync(id);
    }

    public async Task<List<FieldMapping>> GetByTemplateIdAsync(string templateId)
    {
        return await _context.FieldMappings
            .Where(m => m.TemplateId == templateId)
            .ToListAsync();
    }

    public async Task<List<FieldMapping>> GetByTemplateIdOrderedAsync(string templateId)
    {
        return await _context.FieldMappings
            .Where(m => m.TemplateId == templateId)
            .OrderBy(m => m.ExecutionOrder)
            .ToListAsync();
    }

    public async Task<FieldMapping> CreateAsync(FieldMapping mapping)
    {
        mapping.CreatedAt = DateTime.UtcNow;
        mapping.UpdatedAt = DateTime.UtcNow;

        _context.FieldMappings.Add(mapping);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created field mapping: {mapping.SourcePath} -> {mapping.TargetPath}");
        return mapping;
    }

    public async Task<List<FieldMapping>> CreateBulkAsync(List<FieldMapping> mappings)
    {
        var now = DateTime.UtcNow;
        foreach (var mapping in mappings)
        {
            mapping.CreatedAt = now;
            mapping.UpdatedAt = now;
        }

        _context.FieldMappings.AddRange(mappings);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created {mappings.Count} field mappings in bulk");
        return mappings;
    }

    public async Task<FieldMapping> UpdateAsync(FieldMapping mapping)
    {
        mapping.UpdatedAt = DateTime.UtcNow;

        _context.FieldMappings.Update(mapping);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Updated field mapping: {mapping.Id}");
        return mapping;
    }

    public async Task DeleteAsync(string id)
    {
        var mapping = await GetByIdAsync(id);
        if (mapping != null)
        {
            _context.FieldMappings.Remove(mapping);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted field mapping: {id}");
        }
    }

    public async Task DeleteByTemplateIdAsync(string templateId)
    {
        var mappings = await GetByTemplateIdAsync(templateId);
        _context.FieldMappings.RemoveRange(mappings);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Deleted {mappings.Count} field mappings for template: {templateId}");
    }
}
