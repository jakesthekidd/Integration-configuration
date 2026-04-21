using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/lookup-tables")]
[Tags("Lookup Tables")]
public class LookupTablesController : ControllerBase
{
    private readonly ILookupTableRepository _repo;

    public LookupTablesController(ILookupTableRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<LookupTableListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? tmsSystemId = null)
    {
        var lookupTables = !tmsSystemId.HasValue
            ? await _repo.GetAllAsync()
            : await _repo.GetByTmsSystemIdAsync(tmsSystemId.Value);

        var response = new LookupTableListResponse
        {
            LookupTables = lookupTables.Select(l => new LookupTableResponse
            {
                Id = l.Id,
                TmsSystemId = l.TmsSystemId,
                FieldName = l.FieldName,
                Name = l.Name,
                Description = l.Description,
                Mappings = l.Mappings,
                DefaultValue = l.DefaultValue,
                IsCaseSensitive = l.IsCaseSensitive,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                CreatedBy = l.CreatedBy
            }).ToList(),
            TotalCount = lookupTables.Count
        };

        return Ok(ApiResponse<LookupTableListResponse>.SuccessResponse(response));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LookupTableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LookupTableResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var lookupTable = await _repo.GetByIdAsync(id);
        if (lookupTable == null)
        {
            return NotFound(ApiResponse<LookupTableResponse>.ErrorResponse($"Lookup table not found: {id}"));
        }

        var response = new LookupTableResponse
        {
            Id = lookupTable.Id,
            TmsSystemId = lookupTable.TmsSystemId,
            FieldName = lookupTable.FieldName,
            Name = lookupTable.Name,
            Description = lookupTable.Description,
            Mappings = lookupTable.Mappings,
            DefaultValue = lookupTable.DefaultValue,
            IsCaseSensitive = lookupTable.IsCaseSensitive,
            CreatedAt = lookupTable.CreatedAt,
            UpdatedAt = lookupTable.UpdatedAt,
            CreatedBy = lookupTable.CreatedBy
        };

        return Ok(ApiResponse<LookupTableResponse>.SuccessResponse(response));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LookupTableResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateLookupTableRequest request)
    {
        var lookupTable = new LookupTable
        {
            TmsSystemId = request.TmsSystemId,
            FieldName = request.FieldName,
            Name = request.Name,
            Description = request.Description,
            Mappings = request.Mappings,
            DefaultValue = request.DefaultValue,
            IsCaseSensitive = request.IsCaseSensitive
        };

        var created = await _repo.CreateAsync(lookupTable);

        var response = new LookupTableResponse
        {
            Id = created.Id,
            TmsSystemId = created.TmsSystemId,
            FieldName = created.FieldName,
            Name = created.Name,
            Description = created.Description,
            Mappings = created.Mappings,
            DefaultValue = created.DefaultValue,
            IsCaseSensitive = created.IsCaseSensitive,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            CreatedBy = created.CreatedBy
        };

        return Created($"/api/v1/lookup-tables/{created.Id}", ApiResponse<LookupTableResponse>.SuccessResponse(response));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LookupTableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LookupTableResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLookupTableRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(ApiResponse<LookupTableResponse>.ErrorResponse($"Lookup table not found: {id}"));
        }

        existing.FieldName = request.FieldName;
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Mappings = request.Mappings;
        existing.DefaultValue = request.DefaultValue;
        existing.IsCaseSensitive = request.IsCaseSensitive;

        var updated = await _repo.UpdateAsync(existing);

        var response = new LookupTableResponse
        {
            Id = updated.Id,
            TmsSystemId = updated.TmsSystemId,
            FieldName = updated.FieldName,
            Name = updated.Name,
            Description = updated.Description,
            Mappings = updated.Mappings,
            DefaultValue = updated.DefaultValue,
            IsCaseSensitive = updated.IsCaseSensitive,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            CreatedBy = updated.CreatedBy
        };

        return Ok(ApiResponse<LookupTableResponse>.SuccessResponse(response));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse($"Lookup table not found: {id}"));
        }

        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;

        var updated = await _repo.UpdateAsync(existing);
        return NoContent();
    }

}
