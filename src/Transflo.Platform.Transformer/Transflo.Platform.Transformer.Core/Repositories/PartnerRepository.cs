using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.Core.Repositories;

public class PartnerRepository : IPartnerRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<PartnerRepository> _logger;

    public PartnerRepository(FieldMappingDbContext context, ILogger<PartnerRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Partner?> GetByIdAsync(Guid id)
    {
        return await _context.Partners
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Partners
            .AnyAsync(p => p.Name == name && !p.IsDeleted);
    }
    public async Task<List<Partner>> GetAllAsync()
    {
        return await _context.Partners
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<(List<Partner> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = _context.Partners
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Partner> CreateAsync(Partner partner)
    {
        partner.IsDeleted = false;

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created partner: {Name} (ID: {Id})", partner.Name, partner.Id);
        return partner;
    }

    public async Task<Partner> UpdateAsync(Partner partner)
    {
        _context.Partners.Update(partner);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated partner: {Id}", partner.Id);
        return partner;
    }

    public async Task DeleteAsync(Guid id)
    {
        var partner = await _context.Partners.FindAsync(id);
        if (partner is not null)
        {
            partner.IsDeleted = true;
            partner.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Soft-deleted partner: {Id}", id);
        }
    }
}
