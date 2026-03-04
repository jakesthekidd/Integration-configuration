namespace FieldMappingApi.DTOs;

public class CreateTmsSystemRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0";
    public string? SampleJsonSchema { get; set; }
    public string? ConnectionConfig { get; set; }
    public string? Metadata { get; set; }
}

public class UpdateTmsSystemRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;
    public string? SampleJsonSchema { get; set; }
    public string? ConnectionConfig { get; set; }
    public string? Metadata { get; set; }
}

public class TmsSystemResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? SampleJsonSchema { get; set; }
    public string? ConnectionConfig { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? Metadata { get; set; }
}

public class TmsSystemListResponse
{
    public List<TmsSystemResponse> Systems { get; set; } = new();
    public int TotalCount { get; set; }
}
