using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories;

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

public class TmsSystemRepository : ITmsSystemRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<TmsSystemRepository> _logger;

    public TmsSystemRepository(FieldMappingDbContext context, ILogger<TmsSystemRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TmsSystem?> GetByIdAsync(Guid id)
    {
        return await _context.TmsSystems.FindAsync(id);
    }

    public async Task<TmsSystem?> GetByNameAsync(string name)
    {
        return await _context.TmsSystems.FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task<List<TmsSystem>> GetAllAsync()
    {
        return await _context.TmsSystems.ToListAsync();
    }

    public async Task<List<TmsSystem>> GetActiveSystemsAsync()
    {
        return await _context.TmsSystems.Where(t => t.IsActive).ToListAsync();
    }

    public async Task<TmsSystem> CreateAsync(TmsSystem tmsSystem)
    {
        tmsSystem.CreatedAt = DateTime.UtcNow;
        tmsSystem.UpdatedAt = DateTime.UtcNow;

        _context.TmsSystems.Add(tmsSystem);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created TMS system: {tmsSystem.Name} (ID: {tmsSystem.Id})");
        return tmsSystem;
    }

    public async Task<TmsSystem> UpdateAsync(TmsSystem tmsSystem)
    {
        tmsSystem.UpdatedAt = DateTime.UtcNow;

        _context.TmsSystems.Update(tmsSystem);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Updated TMS system: {tmsSystem.Id}");
        return tmsSystem;
    }

    public async Task DeleteAsync(Guid id)
    {
        var tmsSystem = await GetByIdAsync(id);
        if (tmsSystem != null)
        {
            _context.TmsSystems.Remove(tmsSystem);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted TMS system: {id}");
        }
    }
}
