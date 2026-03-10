namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record BatchTransformRequest
{
    public string TemplateId { get; set; } = string.Empty;
    public int? Version { get; set; }
    public string? UserId { get; set; }
    public List<System.Text.Json.JsonElement> Records { get; set; } = new();
}
