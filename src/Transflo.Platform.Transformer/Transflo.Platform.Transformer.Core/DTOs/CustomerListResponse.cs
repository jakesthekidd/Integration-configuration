namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CustomerListResponse
{
    public List<CustomerResponse> Customers { get; set; } = new();
    public int TotalCount { get; set; }
}
