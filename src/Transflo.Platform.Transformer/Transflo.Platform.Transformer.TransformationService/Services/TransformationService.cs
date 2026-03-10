using System.Diagnostics;
using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Services;

public class TransformationService : ITransformationService
{
    private readonly IJsonParserService _jsonParser;
    private readonly ITransformationStrategyFactory _strategyFactory;
    private readonly ILogger<TransformationService> _logger;

    public TransformationService(
        IJsonParserService jsonParser,
        ITransformationStrategyFactory strategyFactory,
        ILogger<TransformationService> logger)
    {
        _jsonParser = jsonParser;
        _strategyFactory = strategyFactory;
        _logger = logger;
    }

    public async Task<TransformationResult> TransformAsync(
        string sourceJson,
        FieldMappingTemplate template,
        List<FieldMapping> mappings)
    {
        return await RunTransformationAsync(sourceJson, template, mappings);
    }

    public async Task<BatchTransformResult> TransformBatchAsync(
        FieldMappingTemplate template,
        List<FieldMapping> mappings,
        List<JsonElement> records)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting batch transformation: {Count} records, template: {TemplateId}", records.Count, template.TemplateId);

        var batchResult = new BatchTransformResult
        {
            TemplateId = template.TemplateId,
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

            var recordResult = await RunTransformationAsync(recordJson, template, mappings);

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

        return batchResult;
    }

    internal static TransformationStatus DetermineStatus(TransformationResult result)
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

    private async Task<TransformationResult> RunTransformationAsync(
        string sourceJson,
        FieldMappingTemplate template,
        List<FieldMapping> mappings)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TransformationResult();

        try
        {
            _logger.LogInformation("Starting transformation with template: {TemplateId}", template.TemplateId);

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
            var mappingResult = await ApplyMappingsAsync(sourceData, targetData, mappings);

            bool hasRequiredErrors = mappingResult.Errors.Any(e => e.ErrorCode == "REQUIRED_FIELD_MISSING");

            result.Success = !hasRequiredErrors;
            result.TransformedData = targetData;
            result.OutputData = targetData;
            result.OutputJson = JsonSerializer.Serialize(targetData, new JsonSerializerOptions { WriteIndented = true });
            result.FieldsMapped = mappingResult.FieldsMapped;
            result.FieldsSkipped = mappingResult.FieldsSkipped;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Warnings = mappingResult.Warnings;
            result.Errors.AddRange(mappingResult.Errors);

            _logger.LogInformation(
                "Transformation completed in {Ms}ms — mapped: {Mapped}, skipped: {Skipped}, warnings: {Warnings}, errors: {Errors}",
                result.ExecutionTimeMs, mappingResult.FieldsMapped, mappingResult.FieldsSkipped,
                mappingResult.Warnings.Count, mappingResult.Errors.Count);

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

    private async Task<MappingResult> ApplyMappingsAsync(
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

        return new MappingResult
        {
            FieldsMapped = fieldsMapped,
            FieldsSkipped = fieldsSkipped,
            Warnings = warnings,
            Errors = errors
        };
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
            }

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
