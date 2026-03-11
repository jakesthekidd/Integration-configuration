using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.Core.Services;

public class TemplatesService : ITemplatesService
{
    private readonly ITemplateRepository _templateRepo;
    private readonly IFieldMappingRepository _mappingRepo;

    public TemplatesService(ITemplateRepository templateRepo, IFieldMappingRepository mappingRepo)
    {
        _templateRepo = templateRepo;
        _mappingRepo = mappingRepo;
    }

    public async Task<TemplateResponse[]> GetAllAsync(Guid? tmsSystemId = null)
    {
        var templates = tmsSystemId.HasValue
            ? await _templateRepo.GetByTmsSystemIdAsync(tmsSystemId.Value)
            : await _templateRepo.GetAllAsync();

        return templates.Select(ToResponse).ToArray();
    }

    public async Task<TemplateResponse?> GetByIdAsync(Guid templateId, int? version = null)
    {
        var template = await _templateRepo.GetByIdAsync(templateId, version);
        return template is null ? null : ToResponse(template);
    }

    public async Task<TemplateResponse> CreateAsync(CreateTemplateRequest request)
    {
        var template = new FieldMappingTemplate
        {
            TemplateId = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            TmsSystemId = request.TmsSystemId,
            CustomerId = request.CustomerId,
            Version = 1,
            Status = TemplateStatus.Draft,
            SampleInputJson = NullIfEmpty(request.SampleInputJson),
            Metadata = NullIfEmpty(request.Metadata)
        };

        var created = await _templateRepo.CreateAsync(template);
        return ToResponse(created);
    }

    public async Task<TemplateResponse?> UpdateAsync(Guid templateId, UpdateTemplateRequest request)
    {
        var existing = await _templateRepo.GetLatestVersionAsync(templateId);
        if (existing is null)
            return null;

        var newVersion = new FieldMappingTemplate
        {
            TemplateId = existing.TemplateId,
            Name = request.Name ?? existing.Name,
            Description = request.Description ?? existing.Description,
            TmsSystemId = existing.TmsSystemId,
            CustomerId = request.CustomerId ?? existing.CustomerId,
            Version = existing.Version + 1,
            Status = request.Status ?? existing.Status,
            SampleInputJson = NullIfEmpty(request.SampleInputJson) ?? existing.SampleInputJson,
            Metadata = NullIfEmpty(request.Metadata) ?? existing.Metadata
        };

        var updated = await _templateRepo.CreateAsync(newVersion);
        return ToResponse(updated);
    }

    public async Task<bool> DeleteAsync(Guid templateId, int? version = null)
    {
        var existing = version.HasValue
            ? await _templateRepo.GetByIdAsync(templateId, version)
            : await _templateRepo.GetLatestVersionAsync(templateId);

        if (existing is null)
            return false;

        await _templateRepo.DeleteAsync(templateId, version);
        return true;
    }

    public async Task<TemplateResponse?> DuplicateAsync(Guid templateId)
    {
        var source = await _templateRepo.GetLatestVersionAsync(templateId);
        if (source is null)
            return null;

        var copy = new FieldMappingTemplate
        {
            TemplateId = Guid.NewGuid(),
            Name = $"{source.Name} - Copy",
            Description = source.Description,
            TmsSystemId = source.TmsSystemId,
            CustomerId = source.CustomerId,
            Version = 1,
            Status = TemplateStatus.Draft,
            SampleInputJson = source.SampleInputJson,
            Metadata = source.Metadata
        };

        var created = await _templateRepo.CreateAsync(copy);

        var sourceMappings = await _mappingRepo.GetByTemplateVersionIdOrderedAsync(source.Id);
        if (sourceMappings.Count > 0)
        {
            var copiedMappings = sourceMappings.Select(m => new FieldMapping
            {
                Id = Guid.NewGuid(),
                TemplateVersionId = created.Id,
                SourcePath = m.SourcePath,
                TargetPath = m.TargetPath,
                TransformationType = m.TransformationType,
                TransformationConfig = m.TransformationConfig,
                ExecutionOrder = m.ExecutionOrder,
                IsRequired = m.IsRequired,
                DefaultValue = m.DefaultValue,
                ValidationRules = m.ValidationRules
            }).ToList();

            await _mappingRepo.CreateBulkAsync(copiedMappings);
        }

        return ToResponse(created);
    }

    /// <summary>Converts an empty or whitespace-only string to null so that jsonb columns
    /// in PostgreSQL are never sent an empty string (which causes error 22P02).</summary>
    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static TemplateResponse ToResponse(FieldMappingTemplate t) => new()
    {
        TemplateId = t.TemplateId,
        Name = t.Name,
        Description = t.Description,
        TmsSystemId = t.TmsSystemId,
        CustomerId = t.CustomerId,
        Version = t.Version,
        Status = t.Status.ToString(),
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        CreatedBy = t.CreatedBy,
        SampleInputJson = t.SampleInputJson,
        Metadata = t.Metadata
    };
}
