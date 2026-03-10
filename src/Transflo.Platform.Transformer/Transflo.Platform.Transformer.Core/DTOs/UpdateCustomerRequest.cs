namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record UpdateCustomerRequest
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}
