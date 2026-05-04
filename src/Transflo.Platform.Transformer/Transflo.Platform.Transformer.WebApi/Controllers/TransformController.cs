using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Microsoft.Extensions.Options;
using Transflo.Platform.Transformer.Core.Configurations;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/transform")]
[Tags("Transformation")]
public class TransformController : ControllerBase
{
    private readonly ITransformationCoordinator _coordinator;
    private readonly ITemplateVersionRepository _templateVersionRepository;
    private readonly IApiClientRepository _apiClientRepository;
    private readonly ApplicationConfiguration _config;

    public TransformController(
        ITransformationCoordinator coordinator,
        ITemplateVersionRepository templateVersionRepository,
        IApiClientRepository apiClientRepository,
        IOptions<ApplicationConfiguration> config)
    {
        _coordinator = coordinator;
        _templateVersionRepository = templateVersionRepository;
        _apiClientRepository = apiClientRepository;
        _config = config.Value;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransformationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TransformationResult>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Transform([FromHeader(Name = "x-client-id")] Guid? clientId, [FromBody] TransformRequest request)
    {
        if (await ValidateClientAccessAsync(clientId, request.TemplateId, request.Version) is { } unauthorized)
        {
            return unauthorized;
        }

        var result = await _coordinator.TransformAsync(request.SourceJson, request.TemplateId, request.Version);
        // Always return 200 with the full result so the client can display ALL errors/warnings at once
        return Ok(ApiResponse<TransformationResult>.SuccessResponse(result, result.MessageSummary));
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(ApiResponse<TransformationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TransformationResult>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Preview([FromHeader(Name = "x-client-id")] Guid? clientId, [FromBody] TransformRequest request)
    {
        if (await ValidateClientAccessAsync(clientId, request.TemplateId, request.Version) is { } unauthorized)
        {
            return unauthorized;
        }

        var result = await _coordinator.PreviewTransformationAsync(request.SourceJson, request.TemplateId, request.Version);
        return Ok(ApiResponse<TransformationResult>.SuccessResponse(result, result.MessageSummary));
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<BatchTransformResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Batch([FromHeader(Name = "x-client-id")] Guid? clientId, [FromBody] BatchTransformRequest request)
    {
        if (request.Records == null || request.Records.Count == 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("'records' must be a non-empty array."));
        }

        if (await ValidateClientAccessAsync(clientId, request.TemplateId, request.Version) is { } unauthorized)
        {
            return unauthorized;
        }

        var result = await _coordinator.TransformBatchAsync(
            request.TemplateId,
            request.Records,
            request.Version,
            new TransformOptions { Source = "BatchAPI", UserId = request.UserId });

        return Ok(ApiResponse<BatchTransformResult>.SuccessResponse(result));
    }

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

        if (targetVersion != null)
        {
            if (targetVersion.Template?.Status == TemplateStatus.Archived)
            {
                return StatusCode(
                    StatusCodes.Status401Unauthorized,
                    ApiResponse<object>.ErrorResponse("Unauthorized. The template associated with this version has been archived."));
            }

            if (!await _templateVersionRepository.HasClientAccessAsync(targetVersion.Id, clientId.Value))
            {
                return StatusCode(
                    StatusCodes.Status401Unauthorized,
                    ApiResponse<object>.ErrorResponse("Unauthorized. API client does not have access to this template version."));
            }
        }

        return null;
    }
}
