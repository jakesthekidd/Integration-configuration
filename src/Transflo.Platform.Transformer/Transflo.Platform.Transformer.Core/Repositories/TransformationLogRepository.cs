using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.DTOs;

namespace Transflo.Platform.Transformer.Core.Repositories;

public interface ITransformationLogRepository
{
    Task<TransformationLog> CreateAsync(TransformationLog log);
    Task<TransformationLog?> GetByIdAsync(Guid id);
    Task<List<TransformationLog>> GetByTemplateIdAsync(Guid templateId, int limit = 50);
    Task<List<TransformationLog>> GetAllAsync(int limit = 100);
}

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

    public async Task<List<TransformationLog>> GetByTemplateIdAsync(Guid templateId, int limit = 50)
    {
        return await _context.TransformationLogs
            .Where(l => l.TemplateId == templateId)
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<TransformationLog>> GetAllAsync(int limit = 100)
    {
        return await _context.TransformationLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
