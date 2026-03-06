using System.Diagnostics;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

namespace Transflo.Platform.Transformer.Core.Services;

public class TransformationService : ITransformationService
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IFieldMappingRepository _mappingRepository;
    private readonly ITransformationLogRepository _logRepository;
    private readonly IJsonParserService _jsonParser;
    private readonly ITransformationStrategyFactory _strategyFactory;
    private readonly ILogger<TransformationService> _logger;

    public TransformationService(
        ITemplateRepository templateRepository,
        IFieldMappingRepository mappingRepository,
        ITransformationLogRepository logRepository,
        IJsonParserService jsonParser,
        ITransformationStrategyFactory strategyFactory,
        ILogger<TransformationService> logger)
    {
        _templateRepository = templateRepository;
        _mappingRepository = mappingRepository;
        _logRepository = logRepository;
        _jsonParser = jsonParser;
        _strategyFactory = strategyFactory;
        _logger = logger;
    }

    public async Task<TransformationResult> TransformAsync(
        string sourceJson,
        string templateId,
        int? version = null,
        TransformOptions? options = null)
    {
        var result = await RunTransformationAsync(sourceJson, templateId, version);
        await PersistLogAsync(sourceJson, templateId, result, options);
        return result;
    }

    public async Task<TransformationResult> PreviewTransformationAsync(
        string sourceJson,
        string templateId,
        int? version = null)
    {
        _logger.LogInformation("Previewing transformation with template: {TemplateId}", templateId);
        return await RunTransformationAsync(sourceJson, templateId, version);
    }

    public async Task<BatchTransformResult> TransformBatchAsync(
        string templateId,
        List<JsonElement> records,
        int? version = null,
        TransformOptions? options = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting batch transformation: {Count} records, template: {TemplateId}", records.Count, templateId);

        var batchResult = new BatchTransformResult
        {
            TemplateId = templateId,
            TotalRecords = records.Count
        };

        for (int i = 0; i < records.Count; i++)
        {
            string recordJson;
            try
            {
                recordJson = records[i].GetRawText();
            }
            catch (Exception ex)
            {
                batchResult.Results.Add(new BatchRecordResult
                {
                    Index = i,
                    Success = false,
                    Errors = new List<TransformationError>
                    {
                        new() { ErrorCode = "INVALID_RECORD", Message = $"Record {i} could not be read: {ex.Message}" }
                    }
                });
                batchResult.ErrorCount++;
                continue;
            }

            var recordResult = await RunTransformationAsync(recordJson, templateId, version);

            var status = DetermineStatus(recordResult);
            if (status == TransformationStatus.Success)
            {
                batchResult.SuccessCount++;
            }
            else if (status == TransformationStatus.Warning)
            {
                batchResult.WarningCount++;
            }
            else if (status == TransformationStatus.PartialSuccess)
            {
                batchResult.PartialSuccessCount++;
            }
            else
            {
                batchResult.ErrorCount++;
            }

            batchResult.Results.Add(new BatchRecordResult
            {
                Index = i,
                Success = recordResult.Success,
                FieldsMapped = recordResult.FieldsMapped,
                FieldsSkipped = recordResult.FieldsSkipped,
                ExecutionTimeMs = recordResult.ExecutionTimeMs,
                OutputData = recordResult.TransformedData,
                Errors = recordResult.Errors,
                Warnings = recordResult.Warnings
            });
        }

        batchResult.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Batch transformation complete in {Ms}ms — success: {S}, warning: {W}, partial: {P}, error: {E}",
            batchResult.TotalExecutionTimeMs,
            batchResult.SuccessCount, batchResult.WarningCount,
            batchResult.PartialSuccessCount, batchResult.ErrorCount);

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

    private async Task<TransformationResult> RunTransformationAsync(
        string sourceJson,
        string templateId,
        int? version)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TransformationResult();

        try
        {
            _logger.LogInformation("Starting transformation with template: {TemplateId}", templateId);

            var template = version.HasValue
                ? await _templateRepository.GetByIdAsync(templateId, version.Value)
                : await _templateRepository.GetLatestVersionAsync(templateId);

            if (template == null)
            {
                result.Success = false;
                result.Errors.Add(new TransformationError
                {
                    ErrorCode = "TEMPLATE_NOT_FOUND",
                    Message = $"Template not found: {templateId}" + (version.HasValue ? $" v{version.Value}" : "")
                });
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            var mappings = await _mappingRepository.GetByTemplateIdOrderedAsync(templateId);

            if (mappings.Count == 0)
            {
                result.Success = false;
                result.Errors.Add(new TransformationError
                {
                    ErrorCode = "NO_MAPPINGS",
                    Message = $"No mappings found for template: {templateId}"
                });
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            var sourceData = JsonSerializer.Deserialize<Dictionary<string, object>>(sourceJson);
            if (sourceData == null)
            {
                result.Success = false;
                result.Errors.Add(new TransformationError
                {
                    ErrorCode = "INVALID_JSON",
                    Message = "Failed to parse source JSON"
                });
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            var targetData = new Dictionary<string, object>();
            var (fieldsMapped, fieldsSkipped, warnings, mappingErrors) =
                await ApplyMappingsAsync(sourceData, targetData, mappings);

            bool hasRequiredErrors = mappingErrors.Any(e => e.ErrorCode == "REQUIRED_FIELD_MISSING");

            result.Success = !hasRequiredErrors;
            result.TransformedData = targetData;
            result.OutputData = targetData;
            result.OutputJson = JsonSerializer.Serialize(targetData, new JsonSerializerOptions { WriteIndented = true });
            result.FieldsMapped = fieldsMapped;
            result.FieldsSkipped = fieldsSkipped;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Warnings = warnings;
            result.Errors.AddRange(mappingErrors);

            _logger.LogInformation(
                "Transformation completed in {Ms}ms — mapped: {Mapped}, skipped: {Skipped}, warnings: {Warnings}, errors: {Errors}",
                result.ExecutionTimeMs, fieldsMapped, fieldsSkipped, warnings.Count, mappingErrors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transformation");

            result.Success = false;
            result.Errors.Add(new TransformationError
            {
                ErrorCode = "TRANSFORMATION_ERROR",
                Message = ex.Message,
                StackTrace = ex.StackTrace
            });
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            return result;
        }
    }

    private async Task PersistLogAsync(
        string sourceJson,
        string templateId,
        TransformationResult result,
        TransformOptions? options)
    {
        try
        {
            var status = DetermineStatus(result);

            var log = new TransformationLog
            {
                Id = Guid.NewGuid().ToString(),
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

    private async Task<(int fieldsMapped, int fieldsSkipped, List<TransformationWarning> warnings, List<TransformationError> errors)> ApplyMappingsAsync(
        Dictionary<string, object> sourceData,
        Dictionary<string, object> targetData,
        List<FieldMapping> mappings)
    {
        int fieldsMapped = 0;
        int fieldsSkipped = 0;
        var warnings = new List<TransformationWarning>();
        var errors = new List<TransformationError>();

        foreach (var mapping in mappings)
        {
            try
            {
                bool success = await ApplyMappingAsync(sourceData, targetData, mapping, warnings, errors);
                if (success)
                {
                    fieldsMapped++;
                }
                else
                {
                    fieldsSkipped++;
                }
            }
            catch (Exception ex)
            {
                // Record but never re-throw — we want all issues collected before returning
                _logger.LogWarning(ex, "Error applying mapping: {Source} -> {Target}", mapping.SourcePath, mapping.TargetPath);
                fieldsSkipped++;
                errors.Add(new TransformationError
                {
                    ErrorCode = "MAPPING_EXCEPTION",
                    FieldPath = mapping.TargetPath,
                    SourcePath = mapping.SourcePath,
                    Message = $"Unexpected error mapping '{mapping.SourcePath}' → '{mapping.TargetPath}': {ex.Message}"
                });
            }
        }

        return (fieldsMapped, fieldsSkipped, warnings, errors);
    }

    private async Task<bool> ApplyMappingAsync(
        Dictionary<string, object> sourceData,
        Dictionary<string, object> targetData,
        FieldMapping mapping,
        List<TransformationWarning> warnings,
        List<TransformationError> errors)
    {
        _logger.LogDebug("Applying mapping: {Source} -> {Target} ({Type})",
            mapping.SourcePath, mapping.TargetPath, mapping.TransformationType);

        var transformationStrategy = _strategyFactory.GetStrategy(mapping.TransformationType);
        object? value;

        if (transformationStrategy != null)
        {
            value = await transformationStrategy.ApplyAsync(new TransformationContext
            {
                SourceData = sourceData,
                Mapping = mapping
            });
        }
        else
        {
            _logger.LogWarning("Unsupported transformation type: {Type}", mapping.TransformationType);
            warnings.Add(new TransformationWarning
            {
                Code = "UNSUPPORTED_TRANSFORMATION_TYPE",
                Message = $"Transformation type '{mapping.TransformationType}' is not supported for " +
                          $"'{mapping.SourcePath}' → '{mapping.TargetPath}'. Falling back to Direct copy.",
                SourcePath = mapping.SourcePath,
                TargetPath = mapping.TargetPath
            });
            value = await _jsonParser.GetValueAtPathAsync(sourceData, mapping.SourcePath);
        }

        if (value == null && !string.IsNullOrEmpty(mapping.DefaultValue))
        {
            value = mapping.DefaultValue;
        }

        if (value == null)
        {
            if (mapping.IsRequired)
            {
                errors.Add(new TransformationError
                {
                    ErrorCode = "REQUIRED_FIELD_MISSING",
                    FieldPath = mapping.TargetPath,
                    SourcePath = mapping.SourcePath,
                    Message = $"Required field '{mapping.TargetPath}' could not be mapped" +
                              (string.IsNullOrEmpty(mapping.SourcePath)
                                  ? "."
                                  : $": source path '{mapping.SourcePath}' returned no value.")
                });
                // Don't throw — continue processing remaining mappings so ALL issues are reported
            }

            // Only warn for fields that have a source path (Constant type has no source)
            if (!string.IsNullOrEmpty(mapping.SourcePath) &&
                mapping.TransformationType != TransformationType.Constant)
            {
                warnings.Add(new TransformationWarning
                {
                    Code = "FIELD_VALUE_MISSING",
                    Message = $"Optional field '{mapping.TargetPath}' was skipped: source path '{mapping.SourcePath}' returned no value.",
                    SourcePath = mapping.SourcePath,
                    TargetPath = mapping.TargetPath
                });
            }

            return false;
        }

        await _jsonParser.SetValueAtPathAsync(targetData, mapping.TargetPath, value);
        return true;
    }
}
