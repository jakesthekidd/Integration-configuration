using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.Core.Repositories;

public class TransformationLogRepository : ITransformationLogRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<TransformationLogRepository> _logger;

    public TransformationLogRepository(FieldMappingDbContext context, ILogger<TransformationLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TransformationLog?> GetByIdAsync(Guid id)
    {
        return await _context.TransformationLogs.FindAsync(id);
    }

    public async Task<TransformationLog> CreateAsync(TransformationLog log)
    {
        _context.TransformationLogs.Add(log);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Transformation log saved: {Id} status={Status}", log.Id, log.Status);
        return log;
    }

    public async Task<List<TransformationLog>> GetByTemplateIdAsync(Guid templateId, int limit = 50, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.TransformationLogs.Where(l => l.TemplateId == templateId);
        query = ApplyDateRange(query, from, to);
        return await query.OrderByDescending(l => l.Timestamp).Take(limit).ToListAsync();
    }

    public async Task<List<TransformationLog>> GetAllAsync(int limit = 100, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(_context.TransformationLogs, from, to);
        return await query.OrderByDescending(l => l.Timestamp).Take(limit).ToListAsync();
    }

    private static IQueryable<TransformationLog> ApplyDateRange(IQueryable<TransformationLog> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue)
        {
            query = query.Where(l => l.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.Timestamp <= to.Value);
        }

        return query;
    }
}
