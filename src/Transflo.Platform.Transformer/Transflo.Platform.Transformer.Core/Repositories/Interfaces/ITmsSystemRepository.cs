using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories.Interfaces;

public interface ITmsSystemRepository
{
    Task<TmsSystem?> GetByIdAsync(Guid id);
    Task<TmsSystem?> GetByNameAsync(string name);
    Task<List<TmsSystem>> GetAllAsync();
    Task<List<TmsSystem>> GetActiveSystemsAsync();
    Task<TmsSystem> CreateAsync(TmsSystem tmsSystem);
    Task<TmsSystem> UpdateAsync(TmsSystem tmsSystem);
    Task DeleteAsync(Guid id);
}
