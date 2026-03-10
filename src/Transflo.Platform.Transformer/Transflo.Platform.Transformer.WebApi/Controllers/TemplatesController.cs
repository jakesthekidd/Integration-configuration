using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/templates")]
[Tags("Templates")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateRepository _repo;

    public TemplatesController(ITemplateRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TemplateListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? tmsSystemId = null)
    {
        var templates = !tmsSystemId.HasValue
            ? await _repo.GetAllAsync()
            : await _repo.GetByTmsSystemIdAsync(tmsSystemId.Value);

        var response = new TemplateListResponse
        {
            Templates = templates.Select(t => new TemplateResponse
            {
                TemplateId = t.TemplateId,
                Name = t.Name,
                Description = t.Description,
                TmsSystemId = t.TmsSystemId,
                CustomerId = t.CustomerId,
                Version = t.Version,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                CreatedBy = t.CreatedBy,
                SampleInputJson = t.SampleInputJson,
                Metadata = t.Metadata
            }).ToList(),
            TotalCount = templates.Count
        };

        return Ok(ApiResponse<TemplateListResponse>.SuccessResponse(response));
    }

    [HttpGet("{templateId}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid templateId, [FromQuery] int? version = null)
    {
        var template = await _repo.GetByIdAsync(templateId, version);
        if (template == null)
        {
            return NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));
        }

        var response = new TemplateResponse
        {
            TemplateId = template.TemplateId,
            Name = template.Name,
            Description = template.Description,
            TmsSystemId = template.TmsSystemId,
            CustomerId = template.CustomerId,
            Version = template.Version,
            Status = template.Status.ToString(),
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            CreatedBy = template.CreatedBy,
            SampleInputJson = template.SampleInputJson,
            Metadata = template.Metadata
        };

        return Ok(ApiResponse<TemplateResponse>.SuccessResponse(response));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request)
    {
        var template = new FieldMappingTemplate
        {
            TemplateId = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            TmsSystemId = request.TmsSystemId,
            CustomerId = request.CustomerId,
            Version = 1,
            Status = TemplateStatus.Draft,
            SampleInputJson = request.SampleInputJson,
            Metadata = request.Metadata
        };

        var created = await _repo.CreateAsync(template);

        var response = new TemplateResponse
        {
            TemplateId = created.TemplateId,
            Name = created.Name,
            Description = created.Description,
            TmsSystemId = created.TmsSystemId,
            CustomerId = created.CustomerId,
            Version = created.Version,
            Status = created.Status.ToString(),
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            CreatedBy = created.CreatedBy,
            SampleInputJson = created.SampleInputJson,
            Metadata = created.Metadata
        };

        return Created($"/api/v1/templates/{created.TemplateId}",
            ApiResponse<TemplateResponse>.SuccessResponse(response));
    }

    [HttpPut("{templateId}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid templateId, [FromBody] UpdateTemplateRequest request)
    {
        var existing = await _repo.GetLatestVersionAsync(templateId);
        if (existing == null)
        {
            return NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));
        }

        // Create new version
        var newVersion = new FieldMappingTemplate
        {
            TemplateId = existing.TemplateId,
            Name = request.Name ?? existing.Name,
            Description = request.Description ?? existing.Description,
            TmsSystemId = existing.TmsSystemId,
            CustomerId = request.CustomerId ?? existing.CustomerId,
            Version = existing.Version + 1,
            Status = request.Status ?? existing.Status,
            SampleInputJson = request.SampleInputJson ?? existing.SampleInputJson,
            Metadata = request.Metadata ?? existing.Metadata
        };

        var updated = await _repo.CreateAsync(newVersion);

        var response = new TemplateResponse
        {
            TemplateId = updated.TemplateId,
            Name = updated.Name,
            Description = updated.Description,
            TmsSystemId = updated.TmsSystemId,
            CustomerId = updated.CustomerId,
            Version = updated.Version,
            Status = updated.Status.ToString(),
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            CreatedBy = updated.CreatedBy,
            SampleInputJson = updated.SampleInputJson,
            Metadata = updated.Metadata
        };

        return Ok(ApiResponse<TemplateResponse>.SuccessResponse(response));
    }

    [HttpDelete("{templateId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid templateId, [FromQuery] int? version = null)
    {
        var existing = version.HasValue
            ? await _repo.GetByIdAsync(templateId, version)
            : await _repo.GetLatestVersionAsync(templateId);

        if (existing == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse($"Template not found: {templateId}"));
        }

        await _repo.DeleteAsync(templateId, version);
        return NoContent();
    }

    [HttpPost("{templateId}/duplicate")]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Duplicate(Guid templateId, [FromServices] IFieldMappingRepository mappingRepo)
    {
        var source = await _repo.GetLatestVersionAsync(templateId);
        if (source == null)
        {
            return NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));
        }

        // Create the duplicate template with a new identity
        var copy = new FieldMappingTemplate
        {
            TemplateId = Guid.NewGuid(),
            Name = $"{source.Name} - Copy",
            Description = source.Description,
            TmsSystemId = source.TmsSystemId,
            CustomerId = source.CustomerId,
            Version = 1,
            Status = TemplateStatus.Draft,
            SampleInputJson = source.SampleInputJson,
            Metadata = source.Metadata
        };

        var createdTemplate = await _repo.CreateAsync(copy);

        // Copy all field mappings from the source template
        var sourceMappings = await mappingRepo.GetByTemplateIdOrderedAsync(templateId);
        if (sourceMappings.Count > 0)
        {
            var copiedMappings = sourceMappings.Select(m => new FieldMapping
            {
                Id = Guid.NewGuid(),
                TemplateId = createdTemplate.TemplateId,
                SourcePath = m.SourcePath,
                TargetPath = m.TargetPath,
                TransformationType = m.TransformationType,
                TransformationConfig = m.TransformationConfig,
                ExecutionOrder = m.ExecutionOrder,
                IsRequired = m.IsRequired,
                DefaultValue = m.DefaultValue,
                ValidationRules = m.ValidationRules
            }).ToList();

            await mappingRepo.CreateBulkAsync(copiedMappings);
        }

        var response = new TemplateResponse
        {
            TemplateId = createdTemplate.TemplateId,
            Name = createdTemplate.Name,
            Description = createdTemplate.Description,
            TmsSystemId = createdTemplate.TmsSystemId,
            CustomerId = createdTemplate.CustomerId,
            Version = createdTemplate.Version,
            Status = createdTemplate.Status.ToString(),
            CreatedAt = createdTemplate.CreatedAt,
            UpdatedAt = createdTemplate.UpdatedAt,
            CreatedBy = createdTemplate.CreatedBy,
            SampleInputJson = createdTemplate.SampleInputJson,
            Metadata = createdTemplate.Metadata
        };

        return Created($"/api/v1/templates/{createdTemplate.TemplateId}",
            ApiResponse<TemplateResponse>.SuccessResponse(response));
    }
}
