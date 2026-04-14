using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/transform-logs")]
[Tags("TransformationLogs")]
public class TransformationLogsController : ControllerBase
{
    private readonly ITransformationLogRepository _repo;
    private readonly ITemplateRepository _templateRepo;

    public TransformationLogsController(ITransformationLogRepository repo, ITemplateRepository templateRepo)
    {
        _repo = repo;
        _templateRepo = templateRepo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? templateId = null,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 100,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        // Npgsql requires DateTimeKind.Utc for "timestamp with time zone" columns.
        // Query-string parsing yields Kind=Unspecified; normalise here so comparisons are correct.
        var fromUtc = from.HasValue ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) : (DateTime?)null;
        var toUtc   = to.HasValue   ? DateTime.SpecifyKind(to.Value,   DateTimeKind.Utc) : (DateTime?)null;

        var logs = templateId.HasValue
            ? await _repo.GetByTemplateIdAsync(templateId.Value, limit, fromUtc, toUtc)
            : await _repo.GetAllAsync(limit, fromUtc, toUtc);

        if (status != null)
            logs = logs.Where(l => l.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

        var templateIds = logs.Select(l => l.TemplateId).Distinct().ToList();
        var templateNames = new Dictionary<Guid, string>();
        foreach (var id in templateIds)
        {
            var template = await _templateRepo.GetByIdAsync(id);
            if (template != null)
            {
                templateNames[id] = template.Name;
            }
        }

        var response = logs.Select(l => new
        {
            l.Id,
            l.TemplateId,
            TemplateName = templateNames.GetValueOrDefault(l.TemplateId),
            l.Timestamp,
            Status = l.Status.ToString(),
            l.MessageSummary,
            l.CorrelationId,
            DurationMs = l.ExecutionTimeMs,
            l.RecordCount,
            l.Source,
            l.UserId,
            l.ExpiresAt,
            HasErrors = l.Errors != null,
            HasOutput = l.OutputData != null
        }).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(new { logs = response, totalCount = response.Count }));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var log = await _repo.GetByIdAsync(id);
        if (log == null) return NotFound(ApiResponse<object>.ErrorResponse("Log entry not found"));

        var template = await _templateRepo.GetByIdAsync(log.TemplateId);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            log.Id,
            log.TemplateId,
            TemplateName = template?.Name,
            log.Timestamp,
            Status = log.Status.ToString(),
            log.MessageSummary,
            log.CorrelationId,
            DurationMs = log.ExecutionTimeMs,
            log.RecordCount,
            log.Source,
            log.UserId,
            log.ExpiresAt,
            log.InputData,
            log.OutputData,
            log.Errors,
            log.Warnings
        }));
    }
}
