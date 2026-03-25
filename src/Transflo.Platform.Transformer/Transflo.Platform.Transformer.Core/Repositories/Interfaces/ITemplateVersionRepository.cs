using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories.Interfaces;

public interface ITemplateVersionRepository
{
    Task<TemplateVersion?> GetByVersionAsync(Guid templateId, int version);
    Task<TemplateVersion?> GetPublishedVersionAsync(Guid templateId);
    Task<List<TemplateVersion>> GetAllVersionsAsync(Guid templateId);
    Task<TemplateVersion> CreateAsync(TemplateVersion version);
    Task<TemplateVersion> UpdateAsync(TemplateVersion version);

    /// <summary>
    /// Atomically marks the currently <see cref="TemplateVersionStatus.Published"/> version as
    /// <see cref="TemplateVersionStatus.Superseded"/> and sets the target version to
    /// <see cref="TemplateVersionStatus.Published"/>. Both changes are saved in a single
    /// <c>SaveChangesAsync</c> call.
    /// </summary>
    Task<TemplateVersion> PublishVersionAsync(Guid templateId, int version, string? publishedBy = null);
}
