using Transflo.Platform.Transformer.Core.DTOs;

namespace Transflo.Platform.Transformer.Core.Services.Interfaces;

public interface IPartnersService
{
    Task<(PartnerResponse[] Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<PartnerResponse?> GetByIdAsync(Guid partnerId);
    Task<PartnerResponse> CreateAsync(CreatePartnerRequest request);
    Task<PartnerResponse?> UpdateAsync(Guid partnerId, UpdatePartnerRequest request);
    /// <summary>Returns false when the partner does not exist.</summary>
    Task<bool> DeleteAsync(Guid partnerId);
}
