using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/partners")]
[Tags("Partners")]
public class PartnersController : ControllerBase
{
    private readonly IPartnersService _service;
    private readonly IPartnerRepository _repository;

    public PartnersController(IPartnersService service, IPartnerRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PartnerListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, totalCount) = await _service.GetAllAsync(page, pageSize);
        var response = new PartnerListResponse
        {
            Partners = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PartnerListResponse>.SuccessResponse(response));
    }

    [HttpGet("{partnerId}")]
    [ProducesResponseType(typeof(ApiResponse<PartnerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PartnerResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid partnerId)
    {
        var response = await _service.GetByIdAsync(partnerId);
        if (response is null)
            return NotFound(ApiResponse<PartnerResponse>.ErrorResponse($"Partner not found: {partnerId}"));

        return Ok(ApiResponse<PartnerResponse>.SuccessResponse(response));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PartnerResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePartnerRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Partner name is required."));
        }

        var duplicate = await _repository.ExistsByNameAsync(request.Name);

        if (duplicate)
        {
            return Conflict(ApiResponse<object>.ErrorResponse(
                $"Partner with name '{request.Name}' already exists."
            ));
        }

        var response = await _service.CreateAsync(request);
        return Created($"/api/v1/partners/{response.Id}",
            ApiResponse<PartnerResponse>.SuccessResponse(response));
    }

    [HttpPut("{partnerId}")]
    [ProducesResponseType(typeof(ApiResponse<PartnerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PartnerResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid partnerId, [FromBody] UpdatePartnerRequest request)
    {
        var response = await _service.UpdateAsync(partnerId, request);
        if (response is null)
            return NotFound(ApiResponse<PartnerResponse>.ErrorResponse($"Partner not found: {partnerId}"));

        return Ok(ApiResponse<PartnerResponse>.SuccessResponse(response));
    }

    [HttpDelete("{partnerId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid partnerId)
    {
        var found = await _service.DeleteAsync(partnerId);
        if (!found)
            return NotFound(ApiResponse<object>.ErrorResponse($"Partner not found: {partnerId}"));

        return NoContent();
    }
}
