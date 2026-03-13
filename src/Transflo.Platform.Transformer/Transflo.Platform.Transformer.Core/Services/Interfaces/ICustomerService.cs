using Transflo.Platform.Transformer.Core.DTOs;

namespace Transflo.Platform.Transformer.Core.Services.Interfaces;

public interface ICustomerService
{
    Task<ApiResponse<CustomerListResponse>> GetCustomersAsync(bool? activeOnly);
    Task<ApiResponse<Customer>> GetCustomerByIdAsync(string id);
    Task<ApiResponse<Customer>> CreateCustomerAsync(CustomerRequest request);
    Task<ApiResponse<Customer>> UpdateCustomerAsync(string id, CustomerRequest updateRequest);
    Task<ApiResponse<bool>> SoftDeleteCustomerAsync(string customerId);
    Task<ApiResponse<Customer>> SetCustomerStatusAsync(string customerId, bool enabled);
}