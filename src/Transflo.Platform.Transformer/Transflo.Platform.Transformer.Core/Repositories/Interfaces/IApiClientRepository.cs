using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories.Interfaces;

public interface IApiClientRepository
{
    Task<List<ApiClient>> GetAllAsync();
    Task<ApiClient?> GetByIdAsync(Guid id);
    Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null);
    Task<ApiClient> CreateAsync(ApiClient client);
    Task<ApiClient> UpdateAsync(ApiClient client);
    Task<bool> DeleteAsync(Guid id);

    // Template Assignments
    Task<List<TemplateVersionResponse>> GetAssignedTemplatesAsync(Guid clientId);
    Task<bool> IsTemplateAssignedAsync(Guid clientId, Guid templateVersionId);
    Task<TemplateVersion?> GetTemplateVersionAsync(Guid templateVersionId);
    Task AssignTemplateAsync(ApiClientTemplateVersion assignment);
    Task<bool> RemoveTemplateAsync(Guid clientId, Guid templateVersionId);
}
