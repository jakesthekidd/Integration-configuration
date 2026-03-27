using Transflo.Platform.Transformer.Core.DTOs;

namespace Transflo.Platform.Transformer.Core.Services.Interfaces;

public interface ITemplatesService
{
    Task<TemplateResponse[]> GetAllAsync();
    Task<TemplateResponse?> GetByIdAsync(Guid templateId);
    Task<TemplateResponse> CreateAsync(CreateTemplateRequest request);
    Task<TemplateResponse?> UpdateAsync(Guid templateId, UpdateTemplateRequest request);
    /// <summary>Returns false when the template does not exist.</summary>
    Task<bool> DeleteAsync(Guid templateId);
    Task<bool> ReactivateAsync(Guid templateId);
    Task<TemplateResponse?> DuplicateAsync(Guid templateId, DuplicateTemplateRequest? request = null);

    // ── Version lifecycle ────────────────────────────────────────────────────

    /// <summary>
    /// Returns all versions for the given template, ordered newest-first.
    /// </summary>
    Task<TemplateVersionResponse[]> GetVersionsAsync(Guid templateId);

    /// <summary>
    /// Creates a new Draft version copied from the currently Published version.
    /// Returns null when no Published version exists yet.
    /// </summary>
    Task<TemplateVersionResponse?> CreateVersionAsync(Guid templateId, CreateVersionRequest? request = null);

    /// <summary>
    /// Publishes the specified Draft version, atomically marking the previously
    /// Published version as Superseded. Returns null when the version is not found
    /// or is not in Draft status.
    /// </summary>
    Task<TemplateVersionResponse?> PublishVersionAsync(Guid templateId, int version, string? publishedBy = null);
    Task<bool> DeleteVersionAsync(Guid templateId, int version);
}
