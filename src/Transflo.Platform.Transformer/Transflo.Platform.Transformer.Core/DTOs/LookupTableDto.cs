namespace Transflo.Platform.Transformer.Core.DTOs;

public class CreateLookupTableRequest
{
    public string TmsSystemId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Mappings { get; set; } // JSON string
    public string? DefaultValue { get; set; }
    public bool IsCaseSensitive { get; set; } = true;
}

public class UpdateLookupTableRequest
{
    public string FieldName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Mappings { get; set; } // JSON string
    public string? DefaultValue { get; set; }
    public bool IsCaseSensitive { get; set; } = true;
}

public class LookupTableResponse
{
    public string Id { get; set; } = string.Empty;
    public string TmsSystemId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Mappings { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsCaseSensitive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class LookupTableListResponse
{
    public List<LookupTableResponse> LookupTables { get; set; } = new();
    public int TotalCount { get; set; }
}
