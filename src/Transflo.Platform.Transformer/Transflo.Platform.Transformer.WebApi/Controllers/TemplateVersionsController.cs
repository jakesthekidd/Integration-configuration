using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Microsoft.Extensions.Options;
using Transflo.Platform.Transformer.Core.Configurations;

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
    private readonly ITransformationCoordinator _coordinator;
    private readonly ITemplateVersionRepository _templateVersionRepository;
    private readonly IApiClientRepository _apiClientRepository;
    private readonly ApplicationConfiguration _config;

    public TemplateVersionsController(
        ITemplatesService service,
        IFieldMappingValidationService validationService,
        ITransformationCoordinator coordinator,
        ITemplateVersionRepository templateVersionRepository,
        IApiClientRepository apiClientRepository,
        IOptions<ApplicationConfiguration> config)
    {
        _service = service;
        _validationService = validationService;
        _coordinator = coordinator;
        _templateVersionRepository = templateVersionRepository;
        _apiClientRepository = apiClientRepository;
        _config = config.Value;
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
    /// Runs the full transformation pipeline for the specified template version against
    /// the provided source document. The source can be a raw JSON object or a
    /// JSON-encoded string — both forms are accepted.
    /// </summary>
    [HttpPost("{version:int}/transform")]
    [ProducesResponseType(typeof(ApiResponse<TransformationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Transform(
        [FromHeader(Name = "x-client-id")] Guid? clientId,
        Guid templateId,
        int version,
        [FromBody] VersionTransformRequest request)
    {
        var sourceJson = ResolveSourceJson(request.SourceDocument);
        if (string.IsNullOrWhiteSpace(sourceJson))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("SourceDocument must not be empty."));
        }

        if (await ValidateClientAccessAsync(clientId, templateId, version) is { } unauthorized)
        {
            return unauthorized;
        }

        var result = await _coordinator.TransformAsync(sourceJson, templateId, version);
        return Ok(ApiResponse<TransformationResult>.SuccessResponse(result, result.MessageSummary));
    }

    /// <summary>
    /// Runs the transformation pipeline in preview mode (no output is persisted) for the
    /// specified template version. Accepts the same source document forms as the transform endpoint.
    /// </summary>
    [HttpPost("{version:int}/transform/preview")]
    [ProducesResponseType(typeof(ApiResponse<TransformationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewTransform(
        Guid templateId,
        int version,
        [FromBody] VersionTransformRequest request)
    {
        var sourceJson = ResolveSourceJson(request.SourceDocument);
        if (string.IsNullOrWhiteSpace(sourceJson))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("SourceDocument must not be empty."));
        }

        var result = await _coordinator.PreviewTransformationAsync(sourceJson, templateId, version);
        return Ok(ApiResponse<TransformationResult>.SuccessResponse(result, result.MessageSummary));
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts a JSON string from a <see cref="JsonElement"/> that may be either a raw
    /// JSON object/array or a JSON-encoded string (i.e. the document was serialized before
    /// being placed in the body).
    /// </summary>
    private static string ResolveSourceJson(JsonElement element) =>
        element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : element.GetRawText();

    /// <summary>
    /// Resolves the target template version and checks whether the given API client has access to it.
    /// Returns a 401 <see cref="IActionResult"/> if access is denied, or <c>null</c> if access is granted.
    /// </summary>
    private async Task<IActionResult?> ValidateClientAccessAsync(Guid? clientId, Guid templateId, int? version)
    {
        if (!clientId.HasValue)
        {
            var origin = Request.Headers["Origin"].ToString();
            var isTrustedOrigin = _config.Cors.AllowedOrigins.Any(o =>
                o.Equals(origin, StringComparison.OrdinalIgnoreCase));

            if (isTrustedOrigin)
            {
                return null;
            }

            return StatusCode(
                StatusCodes.Status401Unauthorized,
                ApiResponse<object>.ErrorResponse("Unauthorized. Missing API client ID for external call."));
        }

        var apiClient = await _apiClientRepository.GetByIdAsync(clientId.Value);
        if (apiClient == null)
        {
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                ApiResponse<object>.ErrorResponse("Unauthorized. API client not found."));
        }

        if (!apiClient.IsActive)
        {
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                ApiResponse<object>.ErrorResponse("Unauthorized. API client is inactive."));
        }

        var targetVersion = version.HasValue
            ? await _templateVersionRepository.GetByVersionAsync(templateId, version.Value)
            : await _templateVersionRepository.GetPublishedVersionAsync(templateId);

        if (targetVersion != null && !await _templateVersionRepository.HasClientAccessAsync(targetVersion.Id, clientId.Value))
        {
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                ApiResponse<object>.ErrorResponse("Unauthorized. API client does not have access to this template version."));
        }

        return null;
    }
}
