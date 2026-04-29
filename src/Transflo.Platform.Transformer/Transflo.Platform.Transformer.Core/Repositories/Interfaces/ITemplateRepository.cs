using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories.Interfaces;

public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid id);
    Task<List<Template>> GetAllAsync();
    Task<(List<Template> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Template> CreateAsync(Template template);
    Task<Template> UpdateAsync(Template template);
    Task DeleteAsync(Guid id);
}
