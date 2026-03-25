using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories.Interfaces;

public interface ILookupTableRepository
{
    Task<LookupTable?> GetByIdAsync(Guid id);
    Task<List<LookupTable>> GetByTmsSystemIdAsync(Guid tmsSystemId);
    Task<LookupTable?> GetByTmsAndFieldAsync(Guid tmsSystemId, string fieldName);
    Task<List<LookupTable>> GetAllAsync();
    Task<LookupTable> CreateAsync(LookupTable lookupTable);
    Task<LookupTable> UpdateAsync(LookupTable lookupTable);
    Task DeleteAsync(Guid id);
}
