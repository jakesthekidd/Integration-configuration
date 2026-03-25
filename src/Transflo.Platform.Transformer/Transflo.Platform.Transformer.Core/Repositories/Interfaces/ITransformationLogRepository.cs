using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories.Interfaces;

public interface ITransformationLogRepository
{
    Task<TransformationLog> CreateAsync(TransformationLog log);
    Task<TransformationLog?> GetByIdAsync(Guid id);
    Task<List<TransformationLog>> GetByTemplateIdAsync(Guid templateId, int limit = 50);
    Task<List<TransformationLog>> GetAllAsync(int limit = 100);
}
