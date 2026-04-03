using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.Core.Services;

public class TemplatesService : ITemplatesService
{
    private readonly ITemplateRepository _templateRepo;
    private readonly IFieldMappingRepository _mappingRepo;
    private readonly ITemplateVersionRepository _versionRepo;
    private readonly IFieldMappingValidationService _validationService;

    public TemplatesService(
        ITemplateRepository templateRepo,
        IFieldMappingRepository mappingRepo,
        ITemplateVersionRepository versionRepo,
        IFieldMappingValidationService validationService)
    {
        _templateRepo = templateRepo;
        _mappingRepo = mappingRepo;
        _versionRepo = versionRepo;
        _validationService = validationService;
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
            Status = TemplateStatus.Active,
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
        {
            return null;
        }

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
        {
            return false;
        }

        await _templateRepo.DeleteAsync(templateId);
        return true;
    }

    public async Task<bool> ReactivateAsync(Guid templateId)
    {
        var existing = await _templateRepo.GetByIdAsync(templateId);
        if (existing is null || existing.IsDeleted) // Specifically don't reactivate soft-deleted
        {
            return false;
        }

        if (existing.Status != TemplateStatus.Archived)
        {
            return false;
        }

        existing.Status = TemplateStatus.Active;
        existing.UpdatedAt = DateTime.UtcNow;

        await _templateRepo.UpdateAsync(existing);
        return true;
    }

    public async Task<TemplateResponse?> DuplicateAsync(Guid templateId, DuplicateTemplateRequest? request = null)
    {
        var includeAllVersions = request?.IncludeAllVersions ?? true;
        var source = await _templateRepo.GetByIdAsync(templateId);
        if (source is null)
        {
            return null;
        }

        var allTemplates = await _templateRepo.GetAllAsync();
        var baseName = $"{source.Name} - Copy";
        var finalName = baseName;

        if (allTemplates.Any(t => string.Equals(t.Name, baseName, StringComparison.OrdinalIgnoreCase)))
        {
            int counter = 1;
            while (allTemplates.Any(t => string.Equals(t.Name, $"{baseName} {counter}", StringComparison.OrdinalIgnoreCase)))
            {
                counter++;
            }
            finalName = $"{baseName} {counter}";
        }

        var copy = new Template
        {
            Id = Guid.NewGuid(),
            Name = finalName,
            Description = source.Description,
            Status = TemplateStatus.Active,
            SourceSchema = source.SourceSchema,
            TargetSchema = source.TargetSchema
        };

        var created = await _templateRepo.CreateAsync(copy);

        if (includeAllVersions)
        {
            var sourceVersions = await _versionRepo.GetAllVersionsAsync(templateId);
            foreach (var sv in sourceVersions.OrderBy(v => v.Version))
            {
                var newVersion = new TemplateVersion
                {
                    TemplateId = created.Id,
                    Version = sv.Version,
                    Status = sv.Status,
                    ValidationRules = sv.ValidationRules,
                    Metadata = sv.Metadata,
                    PublishedAt = sv.PublishedAt,
                    PublishedBy = sv.PublishedBy
                };
                var createdVersion = await _versionRepo.CreateAsync(newVersion);

                var sourceMappings = await _mappingRepo.GetByTemplateVersionIdOrderedAsync(sv.Id);
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
        }
        else
        {

            var allVersions = await _versionRepo.GetAllVersionsAsync(templateId);
            var sourceVersion = allVersions.OrderByDescending(v => v.Version).FirstOrDefault();
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
        }

        // Reload to get versions for the response
        var result = await _templateRepo.GetByIdAsync(created.Id);
        return result is null ? ToResponse(created) : ToResponse(result);
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
        // If a specific base version is requested, use it; otherwise fork from current Published
        TemplateVersion? source;
        if (request?.BaseVersion is int baseVersionNum)
        {
            source = await _versionRepo.GetByVersionAsync(templateId, baseVersionNum);
            if (source is null)
            {
                return null; // Caller will return 404
            }
        }
        else
        {
            // Default: fork from the current Published version
            source = await _versionRepo.GetPublishedVersionAsync(templateId);
            if (source is null)
            {
                return null;
            }
        }

        // Determine next version number (always max+1 regardless of base)
        var allVersions = await _versionRepo.GetAllVersionsAsync(templateId);
        var nextVersion = (allVersions.Any() ? allVersions.Max(v => v.Version) : 0) + 1;

        var newVersion = new TemplateVersion
        {
            TemplateId = templateId,
            Version = nextVersion,
            BaseVersion = source.Version,
            Status = TemplateVersionStatus.Draft,
            ValidationRules = source.ValidationRules,
            Metadata = source.Metadata
        };

        var created = await _versionRepo.CreateAsync(newVersion);

        // Copy all field mappings from the base version
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
        {
            return null;
        }

        var validation = await _validationService.ValidateAsync(templateId, version);
        if (!validation.IsValid)
        {
            return null;
        }

        var published = await _versionRepo.PublishVersionAsync(templateId, version, publishedBy);
        return ToVersionResponse(published);
    }

    public async Task<bool> DeleteVersionAsync(Guid templateId, int version)
    {
        var target = await _versionRepo.GetByVersionAsync(templateId, version);
        if (target is null)
        {
            return false;
        }

        // Restriction: Only Draft versions can be deleted to maintain history
        if (target.Status != TemplateVersionStatus.Draft)
        {
            return false;
        }

        // New Restriction: Do not allow deleting the last version
        var allVersions = await _versionRepo.GetAllVersionsAsync(templateId);
        if (allVersions.Count <= 1)
        {
            return false;
        }

        return await _versionRepo.DeleteAsync(templateId, version);
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
        LatestVersionStatus = t.TemplateVersions?.OrderByDescending(v => v.Version).FirstOrDefault()?.Status.ToString() ?? "Draft",
        SourceSchema = t.SourceSchema,
        TargetSchema = t.TargetSchema,
        Version = t.TemplateVersions?.OrderByDescending(v => v.Version).FirstOrDefault()?.Version ?? 1,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        CreatedBy = "system" // Or mapped if available
    };

    private static TemplateVersionResponse ToVersionResponse(TemplateVersion v) => new()
    {
        Id = v.Id,
        TemplateId = v.TemplateId,
        Version = v.Version,
        BaseVersion = v.BaseVersion,
        Status = v.Status.ToString(),
        ValidationRules = v.ValidationRules,
        Metadata = v.Metadata,
        PublishedAt = v.PublishedAt,
        PublishedBy = v.PublishedBy,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };
}
