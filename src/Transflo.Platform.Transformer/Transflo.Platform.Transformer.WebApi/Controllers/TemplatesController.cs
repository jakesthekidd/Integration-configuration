using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/templates")]
[Tags("Templates")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplatesService _service;

    public TemplatesController(ITemplatesService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TemplateListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _service.GetAllAsync();
        var response = new TemplateListResponse
        {
            Templates = templates.ToList(),
            TotalCount = templates.Length
        };
        return Ok(ApiResponse<TemplateListResponse>.SuccessResponse(response));
    }

    [HttpGet("{templateId}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid templateId)
    {
        var response = await _service.GetByIdAsync(templateId);
        if (response is null)
            return NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));

        return Ok(ApiResponse<TemplateResponse>.SuccessResponse(response));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request)
    {
        var response = await _service.CreateAsync(request);
        return Created($"/api/v1/templates/{response.Id}",
            ApiResponse<TemplateResponse>.SuccessResponse(response));
    }

    [HttpPut("{templateId}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid templateId, [FromBody] UpdateTemplateRequest request)
    {
        var response = await _service.UpdateAsync(templateId, request);
        if (response is null)
            return NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));

        return Ok(ApiResponse<TemplateResponse>.SuccessResponse(response));
    }

    [HttpDelete("{templateId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid templateId)
    {
        var found = await _service.DeleteAsync(templateId);
        if (!found)
            return NotFound(ApiResponse<object>.ErrorResponse($"Template not found: {templateId}"));

        return NoContent();
    }

    [HttpPost("{templateId}/reactivate")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid templateId)
    {
        var found = await _service.ReactivateAsync(templateId);
        if (!found)
            return NotFound(ApiResponse<object>.ErrorResponse($"Template not found or not deleted: {templateId}"));

        return NoContent();
    }

    [HttpPost("{templateId}/duplicate")]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Duplicate(Guid templateId, [FromBody] DuplicateTemplateRequest? request)
    {
        var response = await _service.DuplicateAsync(templateId, request);
        if (response is null)
            return NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));

        return Created($"/api/v1/templates/{response.Id}",
            ApiResponse<TemplateResponse>.SuccessResponse(response));
    }
}
