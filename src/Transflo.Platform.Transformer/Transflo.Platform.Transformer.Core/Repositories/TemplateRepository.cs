using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories;

public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid id);
    Task<List<Template>> GetAllAsync();
    Task<Template> CreateAsync(Template template);
    Task<Template> UpdateAsync(Template template);
    Task DeleteAsync(Guid id);
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

    public async Task<Template?> GetByIdAsync(Guid id)
    {
        return await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Template>> GetAllAsync()
    {
        return await _context.Templates
            .Include(t => t.TemplateVersions)
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Template> CreateAsync(Template template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        template.IsDeleted = false;

        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created template: {template.Name} (ID: {template.Id})");
        return template;
    }

    public async Task<Template> UpdateAsync(Template template)
    {
        template.UpdatedAt = DateTime.UtcNow;

        _context.Templates.Update(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Updated template: {template.Id}");
        return template;
    }

    public async Task DeleteAsync(Guid id)
    {
        var template = await _context.Templates.FindAsync(id);
        if (template is not null)
        {
            template.IsDeleted = true;
            template.DeletedAt = DateTime.UtcNow;
            template.Status = TemplateStatus.Deleted;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Soft-deleted template: {id}");
        }
    }
}
