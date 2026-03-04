using Microsoft.EntityFrameworkCore;
using FieldMappingApi.Data;
using FieldMappingApi.Models;

namespace FieldMappingApi.Repositories;

public interface ILookupTableRepository
{
    Task<LookupTable?> GetByIdAsync(string id);
    Task<List<LookupTable>> GetByTmsSystemIdAsync(string tmsSystemId);
    Task<LookupTable?> GetByTmsAndFieldAsync(string tmsSystemId, string fieldName);
    Task<List<LookupTable>> GetAllAsync();
    Task<LookupTable> CreateAsync(LookupTable lookupTable);
    Task<LookupTable> UpdateAsync(LookupTable lookupTable);
    Task DeleteAsync(string id);
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

    public async Task<LookupTable?> GetByIdAsync(string id)
    {
        return await _context.LookupTables.FindAsync(id);
    }

    public async Task<List<LookupTable>> GetByTmsSystemIdAsync(string tmsSystemId)
    {
        return await _context.LookupTables
            .Where(l => l.TmsSystemId == tmsSystemId)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<LookupTable?> GetByTmsAndFieldAsync(string tmsSystemId, string fieldName)
    {
        return await _context.LookupTables
            .FirstOrDefaultAsync(l => l.TmsSystemId == tmsSystemId && l.FieldName == fieldName);
    }

    public async Task<List<LookupTable>> GetAllAsync()
    {
        return await _context.LookupTables
            .OrderBy(l => l.TmsSystemId)
            .ThenBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<LookupTable> CreateAsync(LookupTable lookupTable)
    {
        lookupTable.CreatedAt = DateTime.UtcNow;
        lookupTable.UpdatedAt = DateTime.UtcNow;

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

    public async Task DeleteAsync(string id)
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
