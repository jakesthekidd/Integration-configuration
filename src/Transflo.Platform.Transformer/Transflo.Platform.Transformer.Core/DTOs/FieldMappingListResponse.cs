namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record FieldMappingListResponse
{
    public List<FieldMappingResponse> Mappings { get; set; } = new();
    public int TotalCount { get; set; }
}
