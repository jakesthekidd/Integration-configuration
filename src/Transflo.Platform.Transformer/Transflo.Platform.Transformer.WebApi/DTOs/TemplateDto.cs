using FieldMappingApi.Models;

namespace FieldMappingApi.DTOs;

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TmsSystemId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}

public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public TemplateStatus? Status { get; set; }
    public string? CustomerId { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}

public class TemplateResponse
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TmsSystemId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}

public class TemplateListResponse
{
    public List<TemplateResponse> Templates { get; set; } = new();
    public int TotalCount { get; set; }
}
