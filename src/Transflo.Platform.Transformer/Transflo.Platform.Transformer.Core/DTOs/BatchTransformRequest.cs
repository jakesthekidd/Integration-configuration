namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record BatchTransformRequest
{
    public Guid TemplateId { get; set; }
    public int? Version { get; set; }
    public Guid? UserId { get; set; }
    public List<System.Text.Json.JsonElement> Records { get; set; } = new();
}
