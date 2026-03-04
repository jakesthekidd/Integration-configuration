using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Services;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/transform")]
[Tags("Transformation")]
public class TransformController : ControllerBase
{
    private readonly ITransformationService _service;

    public TransformController(ITransformationService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransformationResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Transform([FromBody] TransformRequest request)
    {
        var result = await _service.TransformAsync(request.SourceJson, request.TemplateId, request.Version);
        // Always return 200 with the full result so the client can display ALL errors/warnings at once
        return Ok(ApiResponse<TransformationResult>.SuccessResponse(result));
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(ApiResponse<TransformationResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview([FromBody] TransformRequest request)
    {
        var result = await _service.PreviewTransformationAsync(request.SourceJson, request.TemplateId, request.Version);
        return Ok(ApiResponse<TransformationResult>.SuccessResponse(result));
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<BatchTransformResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Batch([FromBody] BatchTransformRequest request)
    {
        if (request.Records == null || request.Records.Count == 0)
            return BadRequest(ApiResponse<object>.ErrorResponse("'records' must be a non-empty array."));

        var result = await _service.TransformBatchAsync(
            request.TemplateId,
            request.Records,
            request.Version,
            new TransformOptions { Source = "BatchAPI", UserId = request.UserId });

        return Ok(ApiResponse<BatchTransformResult>.SuccessResponse(result));
    }
}
