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

    public TransformationLogsController(ITransformationLogRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? templateId = null, [FromQuery] string? status = null, [FromQuery] int limit = 100)
    {
        var logs = templateId.HasValue
            ? await _repo.GetByTemplateIdAsync(templateId.Value, limit)
            : await _repo.GetAllAsync(limit);

        if (status != null)
            logs = logs.Where(l => l.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

        var response = logs.Select(l => new
        {
            l.Id,
            l.TemplateId,
            l.Timestamp,
            Status = l.Status.ToString(),
            l.ExecutionTimeMs,
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

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            log.Id,
            log.TemplateId,
            log.Timestamp,
            Status = log.Status.ToString(),
            log.ExecutionTimeMs,
            log.RecordCount,
            log.Source,
            log.UserId,
            log.ExpiresAt,
            log.InputData,
            log.OutputData,
            log.Errors
        }));
    }
}
