using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories;

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

public class LookupTableRepository : ILookupTableRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<LookupTableRepository> _logger;

    public LookupTableRepository(FieldMappingDbContext context, ILogger<LookupTableRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LookupTable?> GetByIdAsync(Guid id)
    {
        return await _context.LookupTables.FindAsync(id);
    }

    public async Task<List<LookupTable>> GetByTmsSystemIdAsync(Guid tmsSystemId)
    {
        return await _context.LookupTables
            .Where(l => l.TmsSystemId == tmsSystemId && !l.IsDeleted)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<LookupTable?> GetByTmsAndFieldAsync(Guid tmsSystemId, string fieldName)
    {
        return await _context.LookupTables
            .FirstOrDefaultAsync(l => l.TmsSystemId == tmsSystemId && l.FieldName == fieldName);
    }

    public async Task<List<LookupTable>> GetAllAsync()
    {
        return await _context.LookupTables
            .Where(l => !l.IsDeleted)
            .OrderBy(l => l.TmsSystemId)
            .ThenBy(l => l.Name)
            .ToListAsync();
    }
    public async Task<LookupTable> CreateAsync(LookupTable lookupTable)
    {
        lookupTable.CreatedAt = DateTime.UtcNow;
        lookupTable.UpdatedAt = DateTime.UtcNow;
        lookupTable.IsDeleted = false;

        _context.LookupTables.Add(lookupTable);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created lookup table: {lookupTable.Name} (ID: {lookupTable.Id})");
        return lookupTable;
    }

    public async Task<LookupTable> UpdateAsync(LookupTable lookupTable)
    {
        lookupTable.UpdatedAt = DateTime.UtcNow;

        _context.LookupTables.Update(lookupTable);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Updated lookup table: {lookupTable.Id}");
        return lookupTable;
    }

    public async Task DeleteAsync(Guid id)
    {
        var lookupTable = await GetByIdAsync(id);
        if (lookupTable != null)
        {
            _context.LookupTables.Remove(lookupTable);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted lookup table: {id}");
        }
    }
}
