using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.Core.Services;

public class PartnersService : IPartnersService
{
    private readonly IPartnerRepository _repo;

    public PartnersService(IPartnerRepository repo)
    {
        _repo = repo;
    }

    public async Task<(PartnerResponse[] Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var (items, totalCount) = await _repo.GetAllAsync(page, pageSize);
        return (items.Select(ToResponse).ToArray(), totalCount);
    }

    public async Task<PartnerResponse?> GetByIdAsync(Guid partnerId)
    {
        var partner = await _repo.GetByIdAsync(partnerId);
        return partner is null ? null : ToResponse(partner);
    }

    public async Task<PartnerResponse> CreateAsync(CreatePartnerRequest request)
    {
        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description
        };

        var created = await _repo.CreateAsync(partner);
        return ToResponse(created);
    }

    public async Task<PartnerResponse?> UpdateAsync(Guid partnerId, UpdatePartnerRequest request)
    {
        var existing = await _repo.GetByIdAsync(partnerId);
        if (existing is null)
            return null;

        existing.Name = request.Name;
        existing.Description = request.Description;

        var updated = await _repo.UpdateAsync(existing);
        return ToResponse(updated);
    }

    public async Task<bool> DeleteAsync(Guid partnerId)
    {
        var existing = await _repo.GetByIdAsync(partnerId);
        if (existing is null)
            return false;

        await _repo.DeleteAsync(partnerId);
        return true;
    }

    private static PartnerResponse ToResponse(Partner p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
