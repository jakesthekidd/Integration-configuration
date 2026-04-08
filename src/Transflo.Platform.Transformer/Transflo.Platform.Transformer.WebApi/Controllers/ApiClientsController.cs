using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ApiClientsController : ControllerBase
{
    private readonly IApiClientRepository _repository;

    public ApiClientsController(IApiClientRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ApiClientListResponse>>> GetApiClients()
    {
        var clients = await _repository.GetAllAsync();
        var response = clients.Select(MapToResponse).ToList();

        return Ok(ApiResponse<ApiClientListResponse>.SuccessResponse(new ApiClientListResponse
        {
            ApiClients = response,
            TotalCount = response.Count
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ApiClientResponse>>> GetApiClient(Guid id)
    {
        var client = await _repository.GetByIdAsync(id);
        if (client == null)
            return NotFound();

        return Ok(ApiResponse<ApiClientResponse>.SuccessResponse(MapToResponse(client)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ApiClientResponse>>> CreateApiClient(CreateApiClientRequest request)
    {
        if (await _repository.ExistsWithNameAsync(request.Name))
            return BadRequest("An API client with this name already exists.");

        var client = new ApiClient
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedBy = "System" // Should be replaced with actual user
        };

        await _repository.CreateAsync(client);

        return CreatedAtAction(nameof(GetApiClient), new { id = client.Id },
            ApiResponse<ApiClientResponse>.SuccessResponse(MapToResponse(client)));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ApiClientResponse>>> UpdateApiClient(Guid id, UpdateApiClientRequest request)
    {
        if (await _repository.ExistsWithNameAsync(request.Name, excludeId: id))
            return BadRequest("An API client with this name already exists.");

        var client = await _repository.GetByIdAsync(id);
        if (client == null)
            return NotFound();

        client.Name = request.Name;
        client.Description = request.Description;
        client.IsActive = request.IsActive;

        await _repository.UpdateAsync(client);

        return Ok(ApiResponse<ApiClientResponse>.SuccessResponse(MapToResponse(client)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApiClient(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id}/templates")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TemplateVersionResponse>>>> GetAssignedTemplates(Guid id)
    {
        var templates = await _repository.GetAssignedTemplatesAsync(id);
        return Ok(ApiResponse<IEnumerable<TemplateVersionResponse>>.SuccessResponse(templates));
    }

    [HttpPost("{id}/templates")]
    public async Task<IActionResult> AssignTemplate(Guid id, ApiClientTemplateAssignmentRequest request)
    {
        if (await _repository.IsTemplateAssignedAsync(id, request.TemplateVersionId))
            return BadRequest("Template is already assigned to this client.");

        var templateVersion = await _repository.GetTemplateVersionAsync(request.TemplateVersionId);
        if (templateVersion == null)
            return NotFound("Template version not found.");

        if (templateVersion.Status != TemplateVersionStatus.Published &&
            templateVersion.Status != TemplateVersionStatus.Superseded)
        {
            return BadRequest("Only Published or Superseded template versions can be assigned.");
        }

        var assignment = new ApiClientTemplateVersion
        {
            ApiClientId = id,
            TemplateVersionId = request.TemplateVersionId,
            CreatedBy = "System"
        };

        await _repository.AssignTemplateAsync(assignment);

        return NoContent();
    }

    [HttpDelete("{id}/templates/{templateVersionId}")]
    public async Task<IActionResult> RemoveTemplate(Guid id, Guid templateVersionId)
    {
        var removed = await _repository.RemoveTemplateAsync(id, templateVersionId);
        if (!removed)
            return NotFound();

        return NoContent();
    }

    private static ApiClientResponse MapToResponse(ApiClient client) => new()
    {
        Id = client.Id,
        Name = client.Name,
        Description = client.Description,
        IsActive = client.IsActive,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt,
        CreatedBy = client.CreatedBy
    };
}
