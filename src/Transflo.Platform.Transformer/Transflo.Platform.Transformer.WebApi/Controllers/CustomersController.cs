using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Tags("Customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repo;

    public CustomersController(ICustomerRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CustomerListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = null)
    {
        var customers = await _repo.GetAllAsync(activeOnly);

        var response = new CustomerListResponse
        {
            Customers = customers.Select(c => new CustomerResponse
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                ContactEmail = c.ContactEmail,
                ContactPhone = c.ContactPhone,
                IsActive = c.IsActive,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedBy = c.CreatedBy
            }).ToList(),
            TotalCount = customers.Count
        };

        return Ok(ApiResponse<CustomerListResponse>.SuccessResponse(response));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer == null)
            return NotFound(ApiResponse<CustomerResponse>.ErrorResponse($"Customer not found: {id}"));

        var response = new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            Code = customer.Code,
            ContactEmail = customer.ContactEmail,
            ContactPhone = customer.ContactPhone,
            IsActive = customer.IsActive,
            Notes = customer.Notes,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            CreatedBy = customer.CreatedBy
        };

        return Ok(ApiResponse<CustomerResponse>.SuccessResponse(response));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Code = request.Code,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            IsActive = request.IsActive,
            Notes = request.Notes,
            CreatedBy = request.CreatedBy
        };

        var created = await _repo.CreateAsync(customer);

        var response = new CustomerResponse
        {
            Id = created.Id,
            Name = created.Name,
            Code = created.Code,
            ContactEmail = created.ContactEmail,
            ContactPhone = created.ContactPhone,
            IsActive = created.IsActive,
            Notes = created.Notes,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            CreatedBy = created.CreatedBy
        };

        return Created($"/api/v1/customers/{created.Id}", ApiResponse<CustomerResponse>.SuccessResponse(response));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCustomerRequest request)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            return NotFound(ApiResponse<CustomerResponse>.ErrorResponse($"Customer not found: {id}"));

        if (request.Name != null) existing.Name = request.Name;
        if (request.Code != null) existing.Code = request.Code;
        if (request.ContactEmail != null) existing.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) existing.ContactPhone = request.ContactPhone;
        if (request.IsActive.HasValue) existing.IsActive = request.IsActive.Value;
        if (request.Notes != null) existing.Notes = request.Notes;

        var updated = await _repo.UpdateAsync(existing);

        var response = new CustomerResponse
        {
            Id = updated.Id,
            Name = updated.Name,
            Code = updated.Code,
            ContactEmail = updated.ContactEmail,
            ContactPhone = updated.ContactPhone,
            IsActive = updated.IsActive,
            Notes = updated.Notes,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            CreatedBy = updated.CreatedBy
        };

        return Ok(ApiResponse<CustomerResponse>.SuccessResponse(response));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            return NotFound(ApiResponse<object>.ErrorResponse($"Customer not found: {id}"));

        await _repo.DeleteAsync(id);
        return NoContent();
    }
}
