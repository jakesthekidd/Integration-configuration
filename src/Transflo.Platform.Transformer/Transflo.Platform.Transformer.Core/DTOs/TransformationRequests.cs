namespace Transflo.Platform.Transformer.Core.DTOs;

public class TransformRequest
{
    public string SourceJson { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public int? Version { get; set; }
}

public class BatchTransformRequest
{
    /// <summary>The template to apply to every record.</summary>
    public string TemplateId { get; set; } = string.Empty;
    public int? Version { get; set; }
    /// <summary>Optional caller identity forwarded to the log entry.</summary>
    public string? UserId { get; set; }
    /// <summary>Array of TMS JSON objects to transform. Each element is processed independently.</summary>
    public List<System.Text.Json.JsonElement> Records { get; set; } = new();
}

public class JsonParseRequest
{
    public string JsonString { get; set; } = string.Empty;
    public bool IncludeSampleValues { get; set; } = true;
}
