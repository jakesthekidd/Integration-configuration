using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories.Interfaces;

public interface IFieldMappingRepository
{
    Task<FieldMapping?> GetByIdAsync(Guid id);
    Task<List<FieldMapping>> GetByTemplateVersionIdAsync(Guid templateVersionId);
    Task<List<FieldMapping>> GetByTemplateVersionIdOrderedAsync(Guid templateVersionId);
    Task<FieldMapping> CreateAsync(FieldMapping mapping);
    Task<List<FieldMapping>> CreateBulkAsync(List<FieldMapping> mappings);
    Task<FieldMapping> UpdateAsync(FieldMapping mapping);
    Task DeleteAsync(Guid id);
    Task DeleteByTemplateVersionIdAsync(Guid templateVersionId);
}
