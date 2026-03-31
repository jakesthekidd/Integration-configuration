using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

/// <summary>
/// Manages the versioning lifecycle for a template:
/// Draft → Published (previous Published becomes Superseded).
/// </summary>
[ApiController]
[Route("api/v1/templates/{templateId}/versions")]
[Tags("Template Versions")]
public class TemplateVersionsController : ControllerBase
{
    private readonly ITemplatesService _service;
    private readonly IFieldMappingValidationService _validationService;

    public TemplateVersionsController(ITemplatesService service, IFieldMappingValidationService validationService)
    {
        _service = service;
        _validationService = validationService;
    }

    /// <summary>Lists all versions for the template, ordered newest-first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TemplateVersionResponse[]>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersions(Guid templateId)
    {
        var versions = await _service.GetVersionsAsync(templateId);
        return Ok(ApiResponse<TemplateVersionResponse[]>.SuccessResponse(versions));
    }

    /// <summary>Gets a specific version.</summary>
    [HttpGet("{version:int}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateVersionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(Guid templateId, int version)
    {
        var versions = await _service.GetVersionsAsync(templateId);
        var match = versions.FirstOrDefault(v => v.Version == version);
        if (match is null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                $"Version {version} not found for template {templateId}."));
        }

        return Ok(ApiResponse<TemplateVersionResponse>.SuccessResponse(match));
    }

    /// <summary>
    /// Creates a new Draft version forked from the currently Published version,
    /// copying its field mappings. Returns 404 if no Published version exists yet.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TemplateVersionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateVersion(
        Guid templateId,
        [FromBody] CreateVersionRequest? request = null)
    {
        var response = await _service.CreateVersionAsync(templateId, request);
        if (response is null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                $"No Published version found for template {templateId}. Publish a version first before creating a new draft."));
        }

        return Created(
            $"/api/v1/templates/{templateId}/versions/{response.Version}",
            ApiResponse<TemplateVersionResponse>.SuccessResponse(response));
    }

    /// <summary>
    /// Validates all field mappings for the specified version without publishing.
    /// When <c>sourceDocument</c> is included in the request body, field values are
    /// also evaluated against each mapping's ValidationRules.
    /// </summary>
    [HttpPost("{version:int}/validate")]
    [ProducesResponseType(typeof(ApiResponse<MappingValidationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Validate(
        Guid templateId,
        int version,
        [FromBody] ValidateRequest? request = null)
    {
        var result = request?.SourceDocument.HasValue == true
            ? await _validationService.ValidateAsync(templateId, version, request.SourceDocument.Value)
            : await _validationService.ValidateAsync(templateId, version);

        if (result.Issues.Count == 1 && result.Issues[0].Code == ValidationCodes.VersionNotFound)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(result.Issues[0].Message));
        }

        return Ok(ApiResponse<MappingValidationResult>.SuccessResponse(result));
    }

    /// <summary>
    /// Publishes the specified Draft version. The previously Published version
    /// (if any) is automatically marked as Superseded. Returns 404 if the version
    /// does not exist or is not in Draft status. Returns 422 if field-mapping
    /// validation fails, with structured validation issues in the response body.
    /// </summary>
    [HttpPost("{version:int}/publish")]
    [ProducesResponseType(typeof(ApiResponse<TemplateVersionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MappingValidationResult>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(
        Guid templateId,
        int version,
        [FromBody] PublishVersionRequest? request = null)
    {
        var validation = await _validationService.ValidateAsync(templateId, version);

        if (validation.Issues.Count == 1 && validation.Issues[0].Code == ValidationCodes.VersionNotFound)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(validation.Issues[0].Message));
        }

        if (!validation.IsValid)
        {
            var errorCount = validation.Issues.Count(i => i.Severity == ValidationSeverity.Error);
            return UnprocessableEntity(ApiResponse<MappingValidationResult>.SuccessResponse(validation,
                $"Version {version} has {errorCount} validation error(s) and cannot be published."));
        }

        var response = await _service.PublishVersionAsync(templateId, version, request?.PublishedBy);
        if (response is null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                $"Version {version} for template {templateId} was not found or is not in Draft status."));
        }

        return Ok(ApiResponse<TemplateVersionResponse>.SuccessResponse(response));
    }

    /// <summary>
    /// Deletes a Draft version. Returns 400 if the version is not in Draft status.
    /// </summary>
    [HttpDelete("{version:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVersion(Guid templateId, int version)
    {
        var success = await _service.DeleteVersionAsync(templateId, version);
        if (!success)
        {
            var versions = await _service.GetVersionsAsync(templateId);
            var match = versions.FirstOrDefault(v => v.Version == version);

            if (match is null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Version {version} not found."));
            }

            if (versions.Count() <= 1)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("The last remaining version cannot be deleted."));
            }

            return BadRequest(ApiResponse<object>.ErrorResponse("Only Draft versions can be deleted."));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new object(), "Version deleted successfully."));
    }
}
