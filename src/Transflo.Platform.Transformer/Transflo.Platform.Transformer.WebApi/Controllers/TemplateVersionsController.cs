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

    public TemplateVersionsController(ITemplatesService service)
    {
        _service = service;
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
            return NotFound(ApiResponse<object>.ErrorResponse(
                $"Version {version} not found for template {templateId}."));

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
            return NotFound(ApiResponse<object>.ErrorResponse(
                $"No Published version found for template {templateId}. Publish a version first before creating a new draft."));

        return Created(
            $"/api/v1/templates/{templateId}/versions/{response.Version}",
            ApiResponse<TemplateVersionResponse>.SuccessResponse(response));
    }

    /// <summary>
    /// Publishes the specified Draft version. The previously Published version
    /// (if any) is automatically marked as Superseded. Returns 404 if the version
    /// does not exist or is not in Draft status.
    /// </summary>
    [HttpPost("{version:int}/publish")]
    [ProducesResponseType(typeof(ApiResponse<TemplateVersionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(
        Guid templateId,
        int version,
        [FromBody] PublishVersionRequest? request = null)
    {
        var response = await _service.PublishVersionAsync(templateId, version, request?.PublishedBy);
        if (response is null)
            return NotFound(ApiResponse<object>.ErrorResponse(
                $"Version {version} for template {templateId} was not found or is not in Draft status."));

        return Ok(ApiResponse<TemplateVersionResponse>.SuccessResponse(response));
    }
}
