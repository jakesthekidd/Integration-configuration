using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.Core.Services;

public class TemplatesService : ITemplatesService
{
    private readonly ITemplateRepository _templateRepo;
    private readonly IFieldMappingRepository _mappingRepo;
    private readonly ITemplateVersionRepository _versionRepo;

    public TemplatesService(
        ITemplateRepository templateRepo,
        IFieldMappingRepository mappingRepo,
        ITemplateVersionRepository versionRepo)
    {
        _templateRepo = templateRepo;
        _mappingRepo = mappingRepo;
        _versionRepo = versionRepo;
    }

    public async Task<TemplateResponse[]> GetAllAsync()
    {
        var templates = await _templateRepo.GetAllAsync();
        return templates.Select(ToResponse).ToArray();
    }

    public async Task<TemplateResponse?> GetByIdAsync(Guid templateId)
    {
        var template = await _templateRepo.GetByIdAsync(templateId);
        return template is null ? null : ToResponse(template);
    }

    public async Task<TemplateResponse> CreateAsync(CreateTemplateRequest request)
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Status = TemplateStatus.Draft,
            SourceSchema = NullIfEmpty(request.SourceSchema),
            TargetSchema = NullIfEmpty(request.TargetSchema)
        };

        var created = await _templateRepo.CreateAsync(template);

        // Seed the first version
        var version = new TemplateVersion
        {
            TemplateId = created.Id,
            Version = 1,
            Status = TemplateVersionStatus.Draft
        };
        await _versionRepo.CreateAsync(version);

        return ToResponse(created);
    }

    public async Task<TemplateResponse?> UpdateAsync(Guid templateId, UpdateTemplateRequest request)
    {
        var existing = await _templateRepo.GetByIdAsync(templateId);
        if (existing is null)
            return null;

        existing.Name = request.Name ?? existing.Name;
        existing.Description = request.Description ?? existing.Description;
        existing.Status = request.Status ?? existing.Status;
        existing.SourceSchema = NullIfEmpty(request.SourceSchema) ?? existing.SourceSchema;
        existing.TargetSchema = NullIfEmpty(request.TargetSchema) ?? existing.TargetSchema;

        var updated = await _templateRepo.UpdateAsync(existing);
        return ToResponse(updated);
    }

    public async Task<bool> DeleteAsync(Guid templateId)
    {
        var existing = await _templateRepo.GetByIdAsync(templateId);
        if (existing is null)
            return false;

        await _templateRepo.DeleteAsync(templateId);
        return true;
    }

    public async Task<TemplateResponse?> DuplicateAsync(Guid templateId)
    {
        var source = await _templateRepo.GetByIdAsync(templateId);
        if (source is null)
            return null;

        var sourceVersion = await _versionRepo.GetPublishedVersionAsync(templateId);

        var copy = new Template
        {
            Id = Guid.NewGuid(),
            Name = $"{source.Name} - Copy",
            Description = source.Description,
            Status = TemplateStatus.Draft,
            SourceSchema = source.SourceSchema,
            TargetSchema = source.TargetSchema
        };

        var created = await _templateRepo.CreateAsync(copy);

        var newVersion = new TemplateVersion
        {
            TemplateId = created.Id,
            Version = 1,
            Status = TemplateVersionStatus.Draft,
            ValidationRules = sourceVersion?.ValidationRules,
            Metadata = sourceVersion?.Metadata
        };

        var createdVersion = await _versionRepo.CreateAsync(newVersion);

        if (sourceVersion is not null)
        {
            var sourceMappings = await _mappingRepo.GetByTemplateVersionIdOrderedAsync(sourceVersion.Id);
            if (sourceMappings.Count > 0)
            {
                var copiedMappings = sourceMappings.Select(m => new FieldMapping
                {
                    Id = Guid.NewGuid(),
                    TemplateVersionId = createdVersion.Id,
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
        }

        return ToResponse(created);
    }

    public async Task<TemplateVersionResponse[]> GetVersionsAsync(Guid templateId)
    {
        var versions = await _versionRepo.GetAllVersionsAsync(templateId);
        return versions.Select(ToVersionResponse).ToArray();
    }

    public async Task<TemplateVersionResponse?> CreateVersionAsync(
        Guid templateId,
        CreateVersionRequest? request = null)
    {
        // New versions are always forked from the current Published version
        var source = await _versionRepo.GetPublishedVersionAsync(templateId);
        if (source is null)
            return null;

        var newVersion = new TemplateVersion
        {
            TemplateId = templateId,
            Version = source.Version + 1,
            Status = TemplateVersionStatus.Draft,
            ValidationRules = source.ValidationRules,
            Metadata = source.Metadata
        };

        var created = await _versionRepo.CreateAsync(newVersion);

        // Copy all field mappings from the source Published version
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

        return ToVersionResponse(created);
    }

    public async Task<TemplateVersionResponse?> PublishVersionAsync(
        Guid templateId,
        int version,
        string? publishedBy = null)
    {
        var target = await _versionRepo.GetByVersionAsync(templateId, version);
        if (target is null || target.Status != TemplateVersionStatus.Draft)
            return null;

        var published = await _versionRepo.PublishVersionAsync(templateId, version, publishedBy);
        return ToVersionResponse(published);
    }

    /// <summary>Converts an empty or whitespace-only string to null so that jsonb columns
    /// in PostgreSQL are never sent an empty string (which causes error 22P02).</summary>
    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static TemplateResponse ToResponse(Template t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        Status = t.Status.ToString(),
        SourceSchema = t.SourceSchema,
        TargetSchema = t.TargetSchema,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        CreatedBy = "system" // Or mapped if available
    };

    private static TemplateVersionResponse ToVersionResponse(TemplateVersion v) => new()
    {
        Id = v.Id,
        TemplateId = v.TemplateId,
        Version = v.Version,
        Status = v.Status.ToString(),
        ValidationRules = v.ValidationRules,
        Metadata = v.Metadata,
        PublishedAt = v.PublishedAt,
        PublishedBy = v.PublishedBy,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };
}
