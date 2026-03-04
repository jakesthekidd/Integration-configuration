namespace Transflo.Platform.Transformer.Core.DTOs;

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateCustomerRequest
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class CustomerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class CustomerListResponse
{
    public List<CustomerResponse> Customers { get; set; } = new();
    public int TotalCount { get; set; }
}
