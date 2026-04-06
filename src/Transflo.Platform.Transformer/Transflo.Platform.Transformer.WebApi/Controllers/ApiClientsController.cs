using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ApiClientsController : ControllerBase
{
    private readonly FieldMappingDbContext _context;

    public ApiClientsController(FieldMappingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ApiClientListResponse>>> GetApiClients()
    {
        var clients = await _context.ApiClients
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();

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
        var client = await _context.ApiClients.FindAsync(id);

        if (client == null || client.IsDeleted)
        {
            return NotFound();
        }

        return Ok(ApiResponse<ApiClientResponse>.SuccessResponse(MapToResponse(client)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ApiClientResponse>>> CreateApiClient(CreateApiClientRequest request)
    {
        if (await _context.ApiClients.AnyAsync(c => c.Name == request.Name && !c.IsDeleted))
        {
            return BadRequest("An API client with this name already exists.");
        }

        var client = new ApiClient
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedBy = "System" // Should be replaced with actual user
        };

        _context.ApiClients.Add(client);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetApiClient), new { id = client.Id }, ApiResponse<ApiClientResponse>.SuccessResponse(MapToResponse(client)));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ApiClientResponse>>> UpdateApiClient(Guid id, UpdateApiClientRequest request)
    {
        if (await _context.ApiClients.AnyAsync(c => c.Name == request.Name && !c.IsDeleted && c.Id != id))
        {
            return BadRequest("An API client with this name already exists.");
        }

        var client = await _context.ApiClients.FindAsync(id);

        if (client == null || client.IsDeleted)
        {
            return NotFound();
        }

        client.Name = request.Name;
        client.Description = request.Description;
        client.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ApiClientResponse>.SuccessResponse(MapToResponse(client)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApiClient(Guid id)
    {
        var client = await _context.ApiClients.FindAsync(id);

        if (client == null || client.IsDeleted)
        {
            return NotFound();
        }

        client.IsDeleted = true;
        client.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/templates")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TemplateVersionResponse>>>> GetAssignedTemplates(Guid id)
    {
        var assignments = await _context.ApiClientTemplateVersions
            .Include(a => a.TemplateVersion)
                .ThenInclude(v => v!.Template)
            .Where(a => a.ApiClientId == id && !a.IsDeleted)
            .ToListAsync();

        var response = assignments.Select(a => new TemplateVersionResponse
        {
            Id = a.TemplateVersion!.Id,
            TemplateId = a.TemplateVersion.TemplateId,
            TemplateName = a.TemplateVersion.Template!.Name,
            Version = a.TemplateVersion.Version,
            Status = a.TemplateVersion.Status.ToString(),
            PublishedAt = a.TemplateVersion.PublishedAt
        });

        return Ok(ApiResponse<IEnumerable<TemplateVersionResponse>>.SuccessResponse(response));
    }

    [HttpPost("{id}/templates")]
    public async Task<IActionResult> AssignTemplate(Guid id, ApiClientTemplateAssignmentRequest request)
    {
        var exists = await _context.ApiClientTemplateVersions
            .AnyAsync(a => a.ApiClientId == id && a.TemplateVersionId == request.TemplateVersionId && !a.IsDeleted);

        if (exists)
        {
            return BadRequest("Template is already assigned to this client.");
        }

        var templateVersion = await _context.TemplateVersions.FindAsync(request.TemplateVersionId);
        if (templateVersion == null)
        {
            return NotFound("Template version not found.");
        }

        if (templateVersion.Status != TemplateVersionStatus.Published && templateVersion.Status != TemplateVersionStatus.Superseded)
        {
            return BadRequest("Only Published or Superseded template versions can be assigned.");
        }

        var assignment = new ApiClientTemplateVersion
        {
            ApiClientId = id,
            TemplateVersionId = request.TemplateVersionId,
            CreatedBy = "System"
        };

        _context.ApiClientTemplateVersions.Add(assignment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}/templates/{templateVersionId}")]
    public async Task<IActionResult> RemoveTemplate(Guid id, Guid templateVersionId)
    {
        var assignment = await _context.ApiClientTemplateVersions
            .FirstOrDefaultAsync(a => a.ApiClientId == id && a.TemplateVersionId == templateVersionId && !a.IsDeleted);

        if (assignment == null)
        {
            return NotFound();
        }

        assignment.IsDeleted = true;
        assignment.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static ApiClientResponse MapToResponse(ApiClient client)
    {
        return new ApiClientResponse
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
}
