using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/field-mappings")]
[Tags("Field Mappings")]
public class FieldMappingsController : ControllerBase
{
    private readonly IFieldMappingRepository _repo;
    private readonly ITemplateVersionRepository _versionRepo;

    public FieldMappingsController(IFieldMappingRepository repo, ITemplateVersionRepository versionRepo)
    {
        _repo = repo;
        _versionRepo = versionRepo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<FieldMappingListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? templateId = null, [FromQuery] Guid? templateVersionId = null)
    {
        Guid? targetVersionId = templateVersionId;

        if (!targetVersionId.HasValue && templateId.HasValue)
        {
            var versions = await _versionRepo.GetAllVersionsAsync(templateId.Value);
            var draft = versions.FirstOrDefault(v => v.Status == TemplateVersionStatus.Draft);
            var published = versions.FirstOrDefault(v => v.Status == TemplateVersionStatus.Published);
            targetVersionId = draft?.Id ?? published?.Id;
        }

        var mappings = !targetVersionId.HasValue
            ? new List<FieldMapping>() // Return empty if no valid version found
            : await _repo.GetByTemplateVersionIdOrderedAsync(targetVersionId.Value);

        var response = new FieldMappingListResponse
        {
            Mappings = mappings.Select(m => new FieldMappingResponse
            {
                Id = m.Id,
                TemplateId = m.TemplateVersionId,
                SourcePath = m.SourcePath,
                TargetPath = m.TargetPath,
                TransformationType = m.TransformationType.ToString(),
                TransformationConfig = m.TransformationConfig,
                ExecutionOrder = m.ExecutionOrder,
                IsRequired = m.IsRequired,
                DefaultValue = m.DefaultValue,
                ValidationRules = m.ValidationRules,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList(),
            TotalCount = mappings.Count
        };

        return Ok(ApiResponse<FieldMappingListResponse>.SuccessResponse(response));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FieldMappingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FieldMappingResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var mapping = await _repo.GetByIdAsync(id);
        if (mapping == null)
        {
            return NotFound(ApiResponse<FieldMappingResponse>.ErrorResponse($"Field mapping not found: {id}"));
        }

        var response = new FieldMappingResponse
        {
            Id = mapping.Id,
            TemplateId = mapping.TemplateVersionId,
            SourcePath = mapping.SourcePath,
            TargetPath = mapping.TargetPath,
            TransformationType = mapping.TransformationType.ToString(),
            TransformationConfig = mapping.TransformationConfig,
            ExecutionOrder = mapping.ExecutionOrder,
            IsRequired = mapping.IsRequired,
            DefaultValue = mapping.DefaultValue,
            ValidationRules = mapping.ValidationRules,
            CreatedAt = mapping.CreatedAt,
            UpdatedAt = mapping.UpdatedAt
        };

        return Ok(ApiResponse<FieldMappingResponse>.SuccessResponse(response));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FieldMappingResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateFieldMappingRequest request)
    {
        var targetVersionId = request.TemplateVersionId;
        if (!targetVersionId.HasValue)
        {
            var versions = await _versionRepo.GetAllVersionsAsync(request.TemplateId);
            var draft = versions.FirstOrDefault(v => v.Status == TemplateVersionStatus.Draft);

            if (draft == null)
            {
                return BadRequest(ApiResponse<FieldMappingResponse>.ErrorResponse(
                    $"No Draft version found for Template {request.TemplateId}. You must create a new version before adding mappings."));
            }
            targetVersionId = draft.Id;
        }

        var mapping = new FieldMapping
        {
            TemplateVersionId = targetVersionId.Value,
            SourcePath = request.SourcePath,
            TargetPath = request.TargetPath,
            TransformationType = request.TransformationType,
            TransformationConfig = NullIfEmpty(request.TransformationConfig),
            ExecutionOrder = request.ExecutionOrder,
            IsRequired = request.IsRequired,
            DefaultValue = request.DefaultValue,
            ValidationRules = NullIfEmpty(request.ValidationRules)
        };

        var created = await _repo.CreateAsync(mapping);

        var response = new FieldMappingResponse
        {
            Id = created.Id,
            TemplateId = created.TemplateVersionId,
            SourcePath = created.SourcePath,
            TargetPath = created.TargetPath,
            TransformationType = created.TransformationType.ToString(),
            TransformationConfig = created.TransformationConfig,
            ExecutionOrder = created.ExecutionOrder,
            IsRequired = created.IsRequired,
            DefaultValue = created.DefaultValue,
            ValidationRules = created.ValidationRules,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };

        return Created($"/api/v1/field-mappings/{created.Id}", ApiResponse<FieldMappingResponse>.SuccessResponse(response));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FieldMappingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FieldMappingResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFieldMappingRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(ApiResponse<FieldMappingResponse>.ErrorResponse($"Field mapping not found: {id}"));
        }

        existing.SourcePath = request.SourcePath;
        existing.TargetPath = request.TargetPath;
        existing.TransformationType = request.TransformationType;
        existing.TransformationConfig = NullIfEmpty(request.TransformationConfig);
        existing.ExecutionOrder = request.ExecutionOrder;
        existing.IsRequired = request.IsRequired;
        existing.DefaultValue = request.DefaultValue;
        existing.ValidationRules = NullIfEmpty(request.ValidationRules);

        var updated = await _repo.UpdateAsync(existing);

        var response = new FieldMappingResponse
        {
            Id = updated.Id,
            TemplateId = updated.TemplateVersionId,
            SourcePath = updated.SourcePath,
            TargetPath = updated.TargetPath,
            TransformationType = updated.TransformationType.ToString(),
            TransformationConfig = updated.TransformationConfig,
            ExecutionOrder = updated.ExecutionOrder,
            IsRequired = updated.IsRequired,
            DefaultValue = updated.DefaultValue,
            ValidationRules = updated.ValidationRules,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };

        return Ok(ApiResponse<FieldMappingResponse>.SuccessResponse(response));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse($"Field mapping not found: {id}"));
        }


        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;

        var updated = await _repo.UpdateAsync(existing);
        return NoContent();
    }

    /// <summary>Converts an empty or whitespace-only string to null so that jsonb columns
    /// in PostgreSQL are never sent an empty string (which causes error 22P02).</summary>
    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
