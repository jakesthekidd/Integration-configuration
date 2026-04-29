namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TemplateListResponse
{
    public List<TemplateResponse> Templates { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
