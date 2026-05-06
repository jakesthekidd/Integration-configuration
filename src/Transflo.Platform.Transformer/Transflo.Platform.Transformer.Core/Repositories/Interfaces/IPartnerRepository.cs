using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories.Interfaces;

public interface IPartnerRepository
{
    Task<Partner?> GetByIdAsync(Guid id);
    Task<List<Partner>> GetAllAsync();
    Task<(List<Partner> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Partner> CreateAsync(Partner partner);
    Task<Partner> UpdateAsync(Partner partner);
    Task DeleteAsync(Guid id);
}
