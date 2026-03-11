using Transflo.Platform.Transformer.Core.DTOs;

namespace Transflo.Platform.Transformer.Core.Services.Interfaces;

public interface ITemplatesService
{
    Task<TemplateResponse[]> GetAllAsync(Guid? tmsSystemId = null);
    Task<TemplateResponse?> GetByIdAsync(Guid templateId, int? version = null);
    Task<TemplateResponse> CreateAsync(CreateTemplateRequest request);
    Task<TemplateResponse?> UpdateAsync(Guid templateId, UpdateTemplateRequest request);
    /// <summary>Returns false when the template does not exist.</summary>
    Task<bool> DeleteAsync(Guid templateId, int? version = null);
    Task<TemplateResponse?> DuplicateAsync(Guid templateId);
}
