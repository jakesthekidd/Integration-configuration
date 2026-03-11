using System.Text.Json;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using ServiceModels = Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.Core.Services;

public class TransformationCoordinator : ITransformationCoordinator
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IFieldMappingRepository _mappingRepository;
    private readonly ITransformationLogRepository _logRepository;
    private readonly ITransformationService _transformationService;
    private readonly ILogger<TransformationCoordinator> _logger;

    public TransformationCoordinator(
        ITemplateRepository templateRepository,
        IFieldMappingRepository mappingRepository,
        ITransformationLogRepository logRepository,
        ITransformationService transformationService,
        ILogger<TransformationCoordinator> logger)
    {
        _templateRepository = templateRepository;
        _mappingRepository = mappingRepository;
        _logRepository = logRepository;
        _transformationService = transformationService;
        _logger = logger;
    }

    public async Task<TransformationResult> TransformAsync(
        string sourceJson,
        Guid templateId,
        int? version = null,
        TransformOptions? options = null)
    {
        var (template, mappings, earlyResult) = await ResolveAsync(templateId, version);
        if (earlyResult != null)
        {
            return earlyResult;
        }

        var result = await _transformationService.TransformAsync(sourceJson, template!, mappings!);
        await PersistLogAsync(sourceJson, templateId, result, options);
        return result;
    }

    public async Task<TransformationResult> PreviewTransformationAsync(
        string sourceJson,
        Guid templateId,
        int? version = null)
    {
        _logger.LogInformation("Previewing transformation with template: {TemplateId}", templateId);

        var (template, mappings, earlyResult) = await ResolveAsync(templateId, version);
        if (earlyResult != null)
        {
            return earlyResult;
        }

        return await _transformationService.TransformAsync(sourceJson, template!, mappings!);
    }

    public async Task<BatchTransformResult> TransformBatchAsync(
        Guid templateId,
        List<JsonElement> records,
        int? version = null,
        TransformOptions? options = null)
    {
        var (template, mappings, earlyResult) = await ResolveAsync(templateId, version);
        if (earlyResult != null)
        {
            return new BatchTransformResult
            {
                TemplateId = templateId,
                TotalRecords = records.Count,
                ErrorCount = records.Count,
                Results = records.Select((_, i) => new BatchRecordResult
                {
                    Index = i,
                    Success = false,
                    Errors = earlyResult.Errors
                }).ToList()
            };
        }

        var batchResult = await _transformationService.TransformBatchAsync(template!, mappings!, records);

        var summaryResult = new TransformationResult
        {
            Success = batchResult.ErrorCount == 0,
            FieldsMapped = batchResult.Results.Sum(r => r.FieldsMapped),
            FieldsSkipped = batchResult.Results.Sum(r => r.FieldsSkipped),
            ExecutionTimeMs = batchResult.TotalExecutionTimeMs,
            Errors = batchResult.Results.SelectMany(r => r.Errors).ToList(),
            Warnings = batchResult.Results.SelectMany(r => r.Warnings).ToList()
        };
        await PersistLogAsync(
            $"[Batch: {records.Count} records]",
            templateId,
            summaryResult,
            options ?? new TransformOptions { Source = "BatchAPI" });

        return batchResult;
    }

    private async Task<(ServiceModels.FieldMappingTemplate? template, List<ServiceModels.FieldMapping>? mappings, TransformationResult? earlyResult)> ResolveAsync(
        Guid templateId,
        int? version)
    {
        var efTemplate = version.HasValue
            ? await _templateRepository.GetByIdAsync(templateId, version.Value)
            : await _templateRepository.GetLatestVersionAsync(templateId);

        if (efTemplate == null)
        {
            var error = new TransformationResult { Success = false };
            error.Errors.Add(new TransformationError
            {
                ErrorCode = "TEMPLATE_NOT_FOUND",
                Message = $"Template not found: {templateId}" + (version.HasValue ? $" v{version.Value}" : "")
            });
            return (null, null, error);
        }

        var efMappings = await _mappingRepository.GetByTemplateVersionIdOrderedAsync(efTemplate.Id);
        if (efMappings.Count == 0)
        {
            var error = new TransformationResult { Success = false };
            error.Errors.Add(new TransformationError
            {
                ErrorCode = "NO_MAPPINGS",
                Message = $"No mappings found for template: {templateId}"
            });
            return (null, null, error);
        }

        return (ToServiceTemplate(efTemplate), efMappings.Select(ToServiceMapping).ToList(), null);
    }

    private static ServiceModels.FieldMappingTemplate ToServiceTemplate(FieldMappingTemplate ef) =>
        new()
        {
            TemplateId = ef.TemplateId,
            TmsSystemId = ef.TmsSystemId,
            Name = ef.Name,
            Version = ef.Version,
            Description = ef.Description,
            SampleInputJson = ef.SampleInputJson,
            Metadata = ef.Metadata
        };

    private static ServiceModels.FieldMapping ToServiceMapping(FieldMapping ef) =>
        new()
        {
            Id = ef.Id,
            TemplateId = ef.TemplateVersionId,
            SourcePath = ef.SourcePath,
            TargetPath = ef.TargetPath,
            TransformationType = ef.TransformationType,
            TransformationConfig = ef.TransformationConfig,
            ExecutionOrder = ef.ExecutionOrder,
            IsRequired = ef.IsRequired,
            DefaultValue = ef.DefaultValue,
            ValidationRules = ef.ValidationRules
        };

    private async Task PersistLogAsync(
        string sourceJson,
        Guid templateId,
        TransformationResult result,
        TransformOptions? options)
    {
        try
        {
            var status = DetermineStatus(result);

            var log = new TransformationLog
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                Timestamp = DateTime.UtcNow,
                Status = status,
                InputData = sourceJson,
                OutputData = result.OutputJson,
                Errors = result.Errors.Count > 0
                    ? JsonSerializer.Serialize(result.Errors)
                    : null,
                ExecutionTimeMs = result.ExecutionTimeMs,
                RecordCount = 1,
                UserId = options?.UserId,
                Source = options?.Source ?? "API",
                ExpiresAt = DateTime.UtcNow.AddDays(90)
            };

            await _logRepository.CreateAsync(log);
        }
        catch (Exception ex)
        {
            // Logging failure must never break the caller
            _logger.LogError(ex, "Failed to persist transformation log for template {TemplateId}", templateId);
        }
    }

    private static TransformationStatus DetermineStatus(TransformationResult result)
    {
        if (!result.Success)
        {
            return TransformationStatus.Error;
        }

        if (result.Errors.Count > 0)
        {
            return TransformationStatus.PartialSuccess;
        }

        if (result.Warnings.Count > 0)
        {
            return TransformationStatus.Warning;
        }

        return TransformationStatus.Success;
    }
}
