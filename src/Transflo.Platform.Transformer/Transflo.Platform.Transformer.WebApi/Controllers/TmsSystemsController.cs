using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/tms-systems")]
[Tags("TMS Systems")]
public class TmsSystemsController : ControllerBase
{
    private readonly ITmsSystemRepository _repo;

    public TmsSystemsController(ITmsSystemRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TmsSystemListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var systems = activeOnly
            ? await _repo.GetActiveSystemsAsync()
            : await _repo.GetAllAsync();

        var response = new TmsSystemListResponse
        {
            Systems = systems.Select(s => new TmsSystemResponse
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Description = s.Description,
                Version = s.Version,
                IsActive = s.IsActive,
                SampleJsonSchema = s.SampleJsonSchema,
                ConnectionConfig = s.ConnectionConfig,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                CreatedBy = s.CreatedBy,
                Metadata = s.Metadata
            }).ToList(),
            TotalCount = systems.Count
        };

        return Ok(ApiResponse<TmsSystemListResponse>.SuccessResponse(response));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TmsSystemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TmsSystemResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var system = await _repo.GetByIdAsync(id);
        if (system == null)
        {
            return NotFound(ApiResponse<TmsSystemResponse>.ErrorResponse($"TMS system not found: {id}"));
        }

        var response = new TmsSystemResponse
        {
            Id = system.Id,
            Name = system.Name,
            DisplayName = system.DisplayName,
            Description = system.Description,
            Version = system.Version,
            IsActive = system.IsActive,
            SampleJsonSchema = system.SampleJsonSchema,
            ConnectionConfig = system.ConnectionConfig,
            CreatedAt = system.CreatedAt,
            UpdatedAt = system.UpdatedAt,
            CreatedBy = system.CreatedBy,
            Metadata = system.Metadata
        };

        return Ok(ApiResponse<TmsSystemResponse>.SuccessResponse(response));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TmsSystemResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TmsSystemResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTmsSystemRequest request)
    {
        // Check for duplicates
        var existing = await _repo.GetByNameAsync(request.Name);
        if (existing != null)
        {
            return Conflict(ApiResponse<TmsSystemResponse>.ErrorResponse(
                $"A TMS system with name '{request.Name}' already exists"));
        }

        var tmsSystem = new TmsSystem
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Version = request.Version,
            IsActive = true,
            SampleJsonSchema = request.SampleJsonSchema,
            ConnectionConfig = request.ConnectionConfig,
            Metadata = request.Metadata
        };

        var created = await _repo.CreateAsync(tmsSystem);

        var response = new TmsSystemResponse
        {
            Id = created.Id,
            Name = created.Name,
            DisplayName = created.DisplayName,
            Description = created.Description,
            Version = created.Version,
            IsActive = created.IsActive,
            SampleJsonSchema = created.SampleJsonSchema,
            ConnectionConfig = created.ConnectionConfig,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            CreatedBy = created.CreatedBy,
            Metadata = created.Metadata
        };

        return Created($"/api/v1/tms-systems/{created.Id}", ApiResponse<TmsSystemResponse>.SuccessResponse(response));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TmsSystemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TmsSystemResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTmsSystemRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(ApiResponse<TmsSystemResponse>.ErrorResponse($"TMS system not found: {id}"));
        }

        existing.DisplayName = request.DisplayName;
        existing.Description = request.Description;
        existing.Version = request.Version;
        existing.IsActive = request.IsActive;
        existing.SampleJsonSchema = request.SampleJsonSchema;
        existing.ConnectionConfig = request.ConnectionConfig;
        existing.Metadata = request.Metadata;

        var updated = await _repo.UpdateAsync(existing);

        var response = new TmsSystemResponse
        {
            Id = updated.Id,
            Name = updated.Name,
            DisplayName = updated.DisplayName,
            Description = updated.Description,
            Version = updated.Version,
            IsActive = updated.IsActive,
            SampleJsonSchema = updated.SampleJsonSchema,
            ConnectionConfig = updated.ConnectionConfig,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            CreatedBy = updated.CreatedBy,
            Metadata = updated.Metadata
        };

        return Ok(ApiResponse<TmsSystemResponse>.SuccessResponse(response));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse($"TMS system not found: {id}"));
        }

        await _repo.DeleteAsync(id);
        return NoContent();
    }
}
