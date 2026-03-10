namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
}
