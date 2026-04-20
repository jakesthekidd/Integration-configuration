using System.Net.Http.Json;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.Core.Services.CustomerService
{
    public class CustomerService : ICustomerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(HttpClient httpClient, ILogger<CustomerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ApiResponse<CustomerListResponse>> GetCustomersAsync(bool? activeOnly)
        {
            try
            {
                var response = await _httpClient.GetAsync("customers");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>() ?? new List<CustomerResponse>();

                var customers = result
                    .Select(MapToCustomer)
                    .Where(c => !c.IsDeleted && (activeOnly == null || c.Enabled == activeOnly))
                    .ToList();

                _logger.LogInformation("Retrieved {Count} customers (activeOnly={ActiveOnly})", customers.Count, activeOnly);

                var customerResponses = customers.Select(c => new CustomerResponse
                {
                    CustomerId = c.CustomerId,
                    TmsName = c.TmsName,
                    LastSyncTime = c.LastSyncTime,
                    UpdateOrInsertStatuses = c.UpdateOrInsertStatuses,
                    UpdateOnlyStatuses = c.UpdateOnlyStatuses,
                    CustomerName = c.CustomerName,
                    Credentials = c.Credentials,
                    Settings = c.Settings,
                    SyncFrequencyMinutes = c.SyncFrequencyMinutes,
                    OrderRetentionDays = c.OrderRetentionDays,
                    Enabled = c.Enabled,
                    TonuCode = c.TonuCode,
                    OutboundEnabled = c.OutboundEnabled,
                    WhiteListedOrders = c.WhiteListedOrders,
                    SyncBatchSize = c.SyncBatchSize
                }).ToList();

                return ApiResponse<CustomerListResponse>.SuccessResponse(new CustomerListResponse
                {
                    Customers = customerResponses,
                    TotalCount = customerResponses.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customers");
                return ApiResponse<CustomerListResponse>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<Customer>> GetCustomerByIdAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"customers/{id}");
                response.EnsureSuccessStatusCode();

                var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
                if (customer == null)
                {
                    return ApiResponse<Customer>.ErrorResponse("Customer not found");
                }

                return ApiResponse<Customer>.SuccessResponse(MapToCustomer(customer));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch customer by ID: {CustomerId}", id);
                return ApiResponse<Customer>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<Customer>> CreateCustomerAsync(CustomerRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("customers", request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResponse<Customer>.ErrorResponse(body);
                }

                var customer = JsonSerializer.Deserialize<CustomerResponse>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return ApiResponse<Customer>.SuccessResponse(customer != null ? MapToCustomer(customer) : new Customer());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create customer");
                return ApiResponse<Customer>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<Customer>> UpdateCustomerAsync(string id, CustomerRequest updateRequest)
        {
            try
            {
                var existingCustomerResponse = await GetCustomerByIdAsync(updateRequest.CustomerId);

                if (!existingCustomerResponse.Success || existingCustomerResponse.Data == null)
                {
                    return ApiResponse<Customer>.ErrorResponse("Customer not found");
                }

                var response = await _httpClient.PutAsJsonAsync($"customers/{id}", updateRequest);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResponse<Customer>.ErrorResponse(body);
                }

                var customer = JsonSerializer.Deserialize<CustomerResponse>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return ApiResponse<Customer>.SuccessResponse(customer != null ? MapToCustomer(customer) : new Customer());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update customer: {CustomerId}", id);
                return ApiResponse<Customer>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> SoftDeleteCustomerAsync(string customerId)
        {
            try
            {
                var getResponse = await GetCustomerByIdAsync(customerId);
                if (!getResponse.Success || getResponse.Data == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Customer not found");
                }

                var customer = getResponse.Data;
                customer.IsDeleted = true;

                var response = await _httpClient.PutAsJsonAsync($"customers/{customerId}", customer);
                response.EnsureSuccessStatusCode();

                return ApiResponse<bool>.SuccessResponse(true, "Customer deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete customer: {CustomerId}", customerId);
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<Customer>> SetCustomerStatusAsync(string customerId, bool enabled)
        {
            try
            {
                var getResponse = await GetCustomerByIdAsync(customerId);
                if (!getResponse.Success || getResponse.Data == null)
                {
                    return ApiResponse<Customer>.ErrorResponse("Customer not found");
                }

                var customer = getResponse.Data;
                customer.Enabled = enabled;

                var response = await _httpClient.PutAsJsonAsync($"customers/{customerId}", customer);
                response.EnsureSuccessStatusCode();

                var updated = await response.Content.ReadFromJsonAsync<CustomerResponse>();
                if (updated == null)
                {
                    return ApiResponse<Customer>.ErrorResponse("Failed to update status");
                }

                return ApiResponse<Customer>.SuccessResponse(MapToCustomer(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update customer status: {CustomerId}", customerId);
                return ApiResponse<Customer>.ErrorResponse(ex.Message);
            }
        }

        private static Customer MapToCustomer(CustomerResponse dto) => new()
        {
            CustomerId = dto.CustomerId,
            TmsName = dto.TmsName,
            LastSyncTime = dto.LastSyncTime,
            UpdateOrInsertStatuses = dto.UpdateOrInsertStatuses,
            UpdateOnlyStatuses = dto.UpdateOnlyStatuses,
            CustomerName = dto.CustomerName,
            Credentials = dto.Credentials,
            Settings = dto.Settings,
            SyncFrequencyMinutes = dto.SyncFrequencyMinutes,
            OrderRetentionDays = dto.OrderRetentionDays,
            Enabled = dto.Enabled,
            TonuCode = dto.TonuCode,
            OutboundEnabled = dto.OutboundEnabled,
            WhiteListedOrders = dto.WhiteListedOrders,
            SyncBatchSize = dto.SyncBatchSize,
            IsDeleted = dto.IsDeleted
        };
    }
}
