using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.Core.Repositories;

public class ApiClientRepository : IApiClientRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<ApiClientRepository> _logger;

    public ApiClientRepository(FieldMappingDbContext context, ILogger<ApiClientRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ApiClient>> GetAllAsync() =>
        await _context.ApiClients
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<ApiClient?> GetByIdAsync(Guid id)
    {
        var client = await _context.ApiClients.FindAsync(id);
        return (client == null || client.IsDeleted) ? null : client;
    }

    public async Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null) =>
        await _context.ApiClients.AnyAsync(c =>
            c.Name == name &&
            !c.IsDeleted &&
            (excludeId == null || c.Id != excludeId));

    public async Task<ApiClient> CreateAsync(ApiClient client)
    {
        _context.ApiClients.Add(client);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created API client: {Name} (ID: {Id})", client.Name, client.Id);
        return client;
    }

    public async Task<ApiClient> UpdateAsync(ApiClient client)
    {
        _context.ApiClients.Update(client);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated API client: {Id}", client.Id);
        return client;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var client = await _context.ApiClients.FindAsync(id);
        if (client == null || client.IsDeleted)
            return false;

        client.IsDeleted = true;
        client.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Soft-deleted API client: {Id}", id);
        return true;
    }

    // --- Template Assignments ---

    public async Task<List<TemplateVersionResponse>> GetAssignedTemplatesAsync(Guid clientId)
    {
        var assignments = await _context.ApiClientTemplateVersions
            .Include(a => a.TemplateVersion)
                .ThenInclude(v => v!.Template)
            .Where(a => a.ApiClientId == clientId && !a.IsDeleted)
            .ToListAsync();

        return assignments.Select(a => new TemplateVersionResponse
        {
            Id = a.TemplateVersion!.Id,
            TemplateId = a.TemplateVersion.TemplateId,
            TemplateName = a.TemplateVersion.Template!.Name,
            Version = a.TemplateVersion.Version,
            Status = a.TemplateVersion.Status.ToString(),
            PublishedAt = a.TemplateVersion.PublishedAt
        }).ToList();
    }

    public async Task<bool> IsTemplateAssignedAsync(Guid clientId, Guid templateVersionId) =>
        await _context.ApiClientTemplateVersions
            .AnyAsync(a => a.ApiClientId == clientId &&
                           a.TemplateVersionId == templateVersionId &&
                           !a.IsDeleted);

    public async Task<TemplateVersion?> GetTemplateVersionAsync(Guid templateVersionId) =>
        await _context.TemplateVersions.FindAsync(templateVersionId);

    public async Task AssignTemplateAsync(ApiClientTemplateVersion assignment)
    {
        _context.ApiClientTemplateVersions.Add(assignment);
        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Assigned template version {TemplateVersionId} to API client {ClientId}",
            assignment.TemplateVersionId, assignment.ApiClientId);
    }

    public async Task<bool> RemoveTemplateAsync(Guid clientId, Guid templateVersionId)
    {
        var assignment = await _context.ApiClientTemplateVersions
            .FirstOrDefaultAsync(a => a.ApiClientId == clientId &&
                                      a.TemplateVersionId == templateVersionId &&
                                      !a.IsDeleted);

        if (assignment == null)
            return false;

        assignment.IsDeleted = true;
        assignment.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Removed template version {TemplateVersionId} from API client {ClientId}",
            templateVersionId, clientId);
        return true;
    }
}
