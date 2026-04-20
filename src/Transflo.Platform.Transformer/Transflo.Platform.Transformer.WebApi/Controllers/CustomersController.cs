using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

[ApiController]
[Route("api/v1/customers")]
[Tags("Customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CustomerListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CustomerListResponse>>> GetCustomers([FromQuery] bool? activeOnly = null)
    {
        var result = await _customerService.GetCustomersAsync(activeOnly);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Customer>>> GetCustomerById(string id)
    {
        var result = await _customerService.GetCustomerByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Customer>>> CreateCustomer([FromBody] CustomerRequest request)
    {
        var result = await _customerService.CreateCustomerAsync(request);
        return result.Success
            ? CreatedAtAction(nameof(GetCustomerById), new { id = result.Data?.CustomerId }, result)
            : BadRequest(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Customer>>> UpdateCustomer(string id, [FromBody] CustomerRequest request)
    {
        var result = await _customerService.UpdateCustomerAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> SoftDeleteCustomer(string id)
    {
        var result = await _customerService.SoftDeleteCustomerAsync(id);
        return result.Success ? NoContent() : NotFound(result);
    }

    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Customer>>> SetCustomerStatus(string id, [FromQuery] bool enabled)
    {
        var result = await _customerService.SetCustomerStatusAsync(id, enabled);
        return result.Success ? Ok(result) : BadRequest(result);
    }

}
