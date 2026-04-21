using System.Text.Json;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using ServiceModels = Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.Core.Services;

public class TransformationCoordinator : ITransformationCoordinator
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ITemplateRepository _templateRepository;
    private readonly IFieldMappingRepository _mappingRepository;
    private readonly ITransformationLogRepository _logRepository;
    private readonly ITransformationService _transformationService;
    private readonly ITemplateVersionRepository _templateVersionRepository;
    private readonly ILogger<TransformationCoordinator> _logger;

    public TransformationCoordinator(
        ITemplateRepository templateRepository,
        IFieldMappingRepository mappingRepository,
        ITransformationLogRepository logRepository,
        ITransformationService transformationService,
        ITemplateVersionRepository templateVersionRepository,
        ILogger<TransformationCoordinator> logger)
    {
        _templateRepository = templateRepository;
        _mappingRepository = mappingRepository;
        _logRepository = logRepository;
        _transformationService = transformationService;
        _templateVersionRepository = templateVersionRepository;
        _logger = logger;
    }

    public async Task<TransformationResult> TransformAsync(
        string sourceJson,
        Guid templateId,
        int? version = null,
        TransformOptions? options = null)
    {
        var resolution = await ResolveAsync(templateId, version);
        if (resolution.HasError)
        {
            return resolution.EarlyResult!;
        }

        var result = await _transformationService.TransformAsync(sourceJson, resolution.Template!, resolution.Mappings!);
        result.MessageSummary = BuildMessageSummary(DetermineStatus(result), result);
        await PersistLogAsync(sourceJson, templateId, result, options);
        return result;
    }

    public async Task<TransformationResult> PreviewTransformationAsync(
        string sourceJson,
        Guid templateId,
        int? version = null)
    {
        _logger.LogInformation("Previewing transformation with template: {TemplateId}", templateId);

        var resolution = await ResolveAsync(templateId, version);
        if (resolution.HasError)
        {
            return resolution.EarlyResult!;
        }

        var result = await _transformationService.TransformAsync(sourceJson, resolution.Template!, resolution.Mappings!);
        result.MessageSummary = BuildMessageSummary(DetermineStatus(result), result);
        return result;
    }

    public async Task<BatchTransformResult> TransformBatchAsync(
        Guid templateId,
        List<JsonElement> records,
        int? version = null,
        TransformOptions? options = null)
    {
        var resolution = await ResolveAsync(templateId, version);
        if (resolution.HasError)
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
                    Errors = resolution.EarlyResult!.Errors
                }).ToList()
            };
        }

        var batchResult = await _transformationService.TransformBatchAsync(resolution.Template!, resolution.Mappings!, records);

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

    private async Task<TemplateResolutionResult> ResolveAsync(Guid templateId, int? version)
    {
        var efTemplate = await _templateRepository.GetByIdAsync(templateId);
        TemplateVersion? efVersion = null;

        if (version.HasValue)
        {
            efVersion = await _templateVersionRepository.GetByVersionAsync(templateId, version.Value);
        }
        else
        {
            efVersion = await _templateVersionRepository.GetPublishedVersionAsync(templateId);
        }

        if (efTemplate == null || efVersion == null)
        {
            var error = new TransformationResult { Success = false };
            error.Errors.Add(new TransformationError
            {
                ErrorCode = "TEMPLATE_NOT_FOUND",
                Message = $"Template version not found: {templateId}" + (version.HasValue ? $" v{version.Value}" : "")
            });
            return TemplateResolutionResult.Error(error);
        }

        var efMappings = await _mappingRepository.GetByTemplateVersionIdOrderedAsync(efVersion.Id);
        if (efMappings.Count == 0)
        {
            var error = new TransformationResult { Success = false };
            error.Errors.Add(new TransformationError
            {
                ErrorCode = "NO_MAPPINGS",
                Message = $"No mappings found for template: {templateId}"
            });
            return TemplateResolutionResult.Error(error);
        }

        return TemplateResolutionResult.Success(
            ToServiceTemplate(efTemplate),
            efMappings.Select(ToServiceMapping).ToList());
    }

    private static ServiceModels.FieldMappingTemplate ToServiceTemplate(Template ef) =>
        new()
        {
            TemplateId = ef.Id,
            Name = ef.Name,
            Description = ef.Description,
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
                    ? JsonSerializer.Serialize(result.Errors, CamelCaseOptions)
                    : null,
                Warnings = result.Warnings.Count > 0
                    ? JsonSerializer.Serialize(result.Warnings, CamelCaseOptions)
                    : null,
                ExecutionTimeMs = result.ExecutionTimeMs,
                RecordCount = 1,
                UserId = options?.UserId,
                MessageSummary = BuildMessageSummary(status, result),
                CorrelationId = options?.CorrelationId ?? Guid.NewGuid().ToString(),
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

    private static string BuildMessageSummary(TransformationStatus status, TransformationResult result) =>
        status switch
        {
            TransformationStatus.Success =>
                $"Transformed {result.FieldsMapped} field(s) successfully.",
            TransformationStatus.Warning =>
                $"Transformation succeeded with {result.Warnings.Count} warning(s).",
            TransformationStatus.PartialSuccess =>
                $"Transformation partially succeeded: {result.Errors.Count} error(s), {result.Warnings.Count} warning(s).",
            TransformationStatus.Error =>
                result.Errors.Count > 0
                    ? result.Errors[0].Message
                    : "Transformation failed.",
            _ => "Transformation completed."
        };

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
