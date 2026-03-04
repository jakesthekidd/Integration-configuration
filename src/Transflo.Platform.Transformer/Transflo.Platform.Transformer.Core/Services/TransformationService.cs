using System.Diagnostics;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;

namespace Transflo.Platform.Transformer.Core.Services;

// Allows callers to pass optional metadata when triggering a transformation.
public class TransformOptions
{
    public string? Source { get; set; }   // e.g. "WebUI", "API", "Lambda"
    public string? UserId { get; set; }
}

public interface ITransformationService
{
    Task<TransformationResult> TransformAsync(string sourceJson, string templateId, int? version = null, TransformOptions? options = null);
    Task<TransformationResult> PreviewTransformationAsync(string sourceJson, string templateId, int? version = null);
    Task<BatchTransformResult> TransformBatchAsync(string templateId, List<JsonElement> records, int? version = null, TransformOptions? options = null);
}

// ── Batch result types ───────────────────────────────────────────────────────

public class BatchRecordResult
{
    /// <summary>Zero-based index of the record in the input array.</summary>
    public int Index { get; set; }
    public bool Success { get; set; }
    public int FieldsMapped { get; set; }
    public int FieldsSkipped { get; set; }
    public long ExecutionTimeMs { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public List<TransformationError> Errors { get; set; } = new();
    public List<TransformationWarning> Warnings { get; set; } = new();
}

public class BatchTransformResult
{
    public string TemplateId { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int WarningCount { get; set; }
    public int PartialSuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public long TotalExecutionTimeMs { get; set; }
    public List<BatchRecordResult> Results { get; set; } = new();
}

public class TransformationResult
{
    public bool Success { get; set; }
    public Dictionary<string, object>? TransformedData { get; set; }
    public Dictionary<string, object>? OutputData { get; set; } // Alias for backwards compatibility
    public string? OutputJson { get; set; }
    public int FieldsMapped { get; set; }
    public int FieldsSkipped { get; set; }
    public List<TransformationError> Errors { get; set; } = new();
    public List<TransformationWarning> Warnings { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
}

public class TransformationError
{
    public string ErrorCode { get; set; } = string.Empty;
    public string? FieldPath { get; set; }
    public string? SourcePath { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}

public class TransformationWarning
{
    /// <summary>Short machine-readable code, e.g. FIELD_VALUE_MISSING</summary>
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public string? TargetPath { get; set; }
}

public class TransformationService : ITransformationService
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IFieldMappingRepository _mappingRepository;
    private readonly ILookupTableRepository _lookupRepository;
    private readonly ITransformationLogRepository _logRepository;
    private readonly IJsonParserService _jsonParser;
    private readonly ILogger<TransformationService> _logger;

    public TransformationService(
        ITemplateRepository templateRepository,
        IFieldMappingRepository mappingRepository,
        ILookupTableRepository lookupRepository,
        ITransformationLogRepository logRepository,
        IJsonParserService jsonParser,
        ILogger<TransformationService> logger)
    {
        _templateRepository = templateRepository;
        _mappingRepository = mappingRepository;
        _lookupRepository = lookupRepository;
        _logRepository = logRepository;
        _jsonParser = jsonParser;
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
        // Preview runs the full transformation pipeline but does NOT write a log entry.
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
                // If we can't even read the raw JSON, produce a hard error for this record
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
            if (status == TransformationStatus.Success) batchResult.SuccessCount++;
            else if (status == TransformationStatus.Warning) batchResult.WarningCount++;
            else if (status == TransformationStatus.PartialSuccess) batchResult.PartialSuccessCount++;
            else batchResult.ErrorCount++;

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

        // Persist a single summary log entry for the entire batch
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

    // ── Core transformation logic ────────────────────────────────────────────
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

    // ── Persist a log entry to the database ─────────────────────────────────
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
            return TransformationStatus.Error;

        if (result.Errors.Count > 0)
            return TransformationStatus.PartialSuccess;

        if (result.Warnings.Count > 0)
            return TransformationStatus.Warning;

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
                if (success) fieldsMapped++;
                else fieldsSkipped++;
            }
            catch (Exception ex)
            {
                // Record but never re-throw — we want all issues collected before returning
                _logger.LogWarning(ex, $"Error applying mapping: {mapping.SourcePath} -> {mapping.TargetPath}");
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
        _logger.LogDebug($"Applying mapping: {mapping.SourcePath} -> {mapping.TargetPath} ({mapping.TransformationType})");

        object? value = null;

        switch (mapping.TransformationType)
        {
            case TransformationType.Direct:
                value = await _jsonParser.GetValueAtPathAsync(sourceData, mapping.SourcePath);
                break;

            case TransformationType.Constant:
                value = mapping.DefaultValue;
                break;

            case TransformationType.Lookup:
                var sourceValue = await _jsonParser.GetValueAtPathAsync(sourceData, mapping.SourcePath);
                value = await ApplyLookupAsync(sourceValue, mapping);
                break;

            case TransformationType.Concat:
                value = await ApplyConcatAsync(sourceData, mapping);
                break;

            case TransformationType.DateFormat:
                var rawDate = await _jsonParser.GetValueAtPathAsync(sourceData, mapping.SourcePath);
                value = ApplyDateFormat(rawDate, ParseConfig(mapping.TransformationConfig));
                break;

            case TransformationType.ArrayMap:
                value = await ApplyArrayMapAsync(sourceData, mapping);
                break;

            case TransformationType.ArrayFlatten:
                value = await ApplyArrayFlattenAsync(sourceData, mapping);
                break;

            default:
                _logger.LogWarning($"Unsupported transformation type: {mapping.TransformationType}");
                warnings.Add(new TransformationWarning
                {
                    Code = "UNSUPPORTED_TRANSFORMATION_TYPE",
                    Message = $"Transformation type '{mapping.TransformationType}' is not supported for " +
                              $"'{mapping.SourcePath}' → '{mapping.TargetPath}'. Falling back to Direct copy.",
                    SourcePath = mapping.SourcePath,
                    TargetPath = mapping.TargetPath
                });
                value = await _jsonParser.GetValueAtPathAsync(sourceData, mapping.SourcePath);
                break;
        }

        // Apply default value if null and default is specified
        if (value == null && !string.IsNullOrEmpty(mapping.DefaultValue))
        {
            value = mapping.DefaultValue;
        }

        // Report missing values
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

            return false; // Skipped
        }

        // Set value in target
        await _jsonParser.SetValueAtPathAsync(targetData, mapping.TargetPath, value);
        return true; // Successfully mapped
    }

    /// <summary>Parses the JSON-string transformation config into a plain dictionary.</summary>
    private static Dictionary<string, object>? ParseConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return null;
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(configJson); }
        catch { return null; }
    }

    private async Task<string?> ApplyConcatAsync(Dictionary<string, object> sourceData, FieldMapping mapping)
    {
        var config = ParseConfig(mapping.TransformationConfig);
        if (config == null) return null;

        var fields = config.TryGetValue("Fields", out var f) ? f : null;
        if (fields == null) return null;

        var fieldPaths = fields is JsonElement je && je.ValueKind == JsonValueKind.Array
            ? je.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList()
            : new List<string>();

        var separator = config.TryGetValue("Separator", out var sep) ? sep?.ToString() ?? " " : " ";
        var skipEmpty = config.TryGetValue("SkipEmpty", out var skip) && skip?.ToString()?.ToLowerInvariant() == "true";

        var values = new List<string>();
        foreach (var path in fieldPaths)
        {
            var val = await _jsonParser.GetValueAtPathAsync(sourceData, path);
            var str = val is JsonElement elem ? elem.GetString() : val?.ToString();
            if (skipEmpty && string.IsNullOrWhiteSpace(str)) continue;
            if (str != null) values.Add(str);
        }

        return string.Join(separator, values);
    }

    /// <summary>
    /// Converts a date string to the target format.
    /// config keys: DateInputFormat, DateOutputFormat.
    /// </summary>
    private object? ApplyDateFormat(object? rawValue, Dictionary<string, object>? config)
    {
        if (rawValue == null) return null;

        var input = rawValue is JsonElement je ? je.GetString() : rawValue.ToString();
        if (string.IsNullOrWhiteSpace(input)) return null;

        var inputFormat = config?.TryGetValue("DateInputFormat", out var inf) == true ? inf?.ToString() : null;
        var outputFormat = config?.TryGetValue("DateOutputFormat", out var outf) == true ? outf?.ToString() ?? "o" : "o";

        DateTimeOffset parsed;

        if (!string.IsNullOrEmpty(inputFormat))
        {
            var formatsToTry = new[] { inputFormat, inputFormat.Replace("zzz", "zz") };
            if (!DateTimeOffset.TryParseExact(input, formatsToTry,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out parsed))
            {
                if (!DateTimeOffset.TryParse(input,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out parsed))
                {
                    _logger.LogWarning($"Could not parse date '{input}' with format '{inputFormat}'");
                    return input;
                }
            }
        }
        else
        {
            if (!DateTimeOffset.TryParse(input,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out parsed))
            {
                _logger.LogWarning($"Could not parse date '{input}'");
                return input;
            }
        }

        return outputFormat == "o"
            ? parsed.UtcDateTime.ToString("o")
            : parsed.ToString(outputFormat, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Maps every element of a source array to a scalar field value, producing a new array.
    /// Source path uses [*] wildcard, e.g. "movement[*].movement_id".
    /// </summary>
    private async Task<object?> ApplyArrayMapAsync(Dictionary<string, object> sourceData, FieldMapping mapping)
    {
        var sourcePath = mapping.SourcePath ?? string.Empty;
        var wi = sourcePath.IndexOf("[*]", StringComparison.Ordinal);
        if (wi < 0)
        {
            _logger.LogWarning($"ArrayMap source path must contain [*]: {sourcePath}");
            return null;
        }

        var arrayName = sourcePath[..wi];
        var itemField = wi + 3 < sourcePath.Length ? sourcePath[(wi + 4)..] : string.Empty;

        var arrayValue = await _jsonParser.GetValueAtPathAsync(sourceData, arrayName);

        IEnumerable<object?> items = arrayValue switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Array => je.EnumerateArray().Cast<object?>(),
            System.Collections.IList il => il.Cast<object?>(),
            _ => Enumerable.Empty<object?>()
        };

        var result = new List<object>();
        foreach (var item in items)
        {
            if (item == null) continue;
            object? extracted = null;

            if (!string.IsNullOrEmpty(itemField))
            {
                if (item is JsonElement elem && elem.ValueKind == JsonValueKind.Object)
                    extracted = elem.TryGetProperty(itemField, out var p)
                        ? (p.ValueKind == JsonValueKind.String ? p.GetString() : (object?)p.GetRawText())
                        : null;
                else if (item is Dictionary<string, object> d)
                    d.TryGetValue(itemField, out extracted);
            }
            else
            {
                extracted = item;
            }

            if (extracted != null) result.Add(extracted);
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Collects a named field from every element of a source array into a single flat list.
    /// </summary>
    private async Task<object?> ApplyArrayFlattenAsync(Dictionary<string, object> sourceData, FieldMapping mapping)
    {
        var config = ParseConfig(mapping.TransformationConfig);
        var arrayName = config?.TryGetValue("SourceArrayPath", out var ap) == true ? ap?.ToString() : null;
        var itemField = config?.TryGetValue("ItemField", out var fi) == true ? fi?.ToString() : null;

        if (string.IsNullOrEmpty(arrayName))
        {
            var sourcePath = mapping.SourcePath ?? string.Empty;
            var wi = sourcePath.IndexOf("[*]", StringComparison.Ordinal);
            if (wi >= 0)
            {
                arrayName = sourcePath[..wi];
                itemField ??= wi + 3 < sourcePath.Length ? sourcePath[(wi + 4)..] : null;
            }
        }

        if (string.IsNullOrEmpty(arrayName)) return null;

        var arrayValue = await _jsonParser.GetValueAtPathAsync(sourceData, arrayName);

        IEnumerable<object?> items = arrayValue switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Array => je.EnumerateArray().Cast<object?>(),
            System.Collections.IList il => il.Cast<object?>(),
            _ => Enumerable.Empty<object?>()
        };

        bool filterEmpty = config?.TryGetValue("FilterEmpty", out var fe) == true
            && fe?.ToString()?.ToLowerInvariant() == "true";

        var result = new List<object>();
        foreach (var item in items)
        {
            if (item == null) continue;
            object? val = null;

            if (!string.IsNullOrEmpty(itemField))
            {
                if (item is JsonElement elem && elem.ValueKind == JsonValueKind.Object)
                    val = elem.TryGetProperty(itemField, out var p)
                        ? (p.ValueKind == JsonValueKind.String ? (object?)p.GetString() : p.GetRawText())
                        : null;
                else if (item is Dictionary<string, object> d)
                    d.TryGetValue(itemField, out val);
            }
            else
            {
                val = item;
            }

            if (val == null || (filterEmpty && string.IsNullOrWhiteSpace(val.ToString()))) continue;
            result.Add(val);
        }

        return result.Count > 0 ? result : null;
    }

    private async Task<object?> ApplyLookupAsync(object? sourceValue, FieldMapping mapping)
    {
        if (sourceValue == null) return null;

        // Parse transformation config to get lookup table ID
        var config = mapping.TransformationConfig != null
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(mapping.TransformationConfig)
            : null;

        if (config == null || !config.ContainsKey("LookupTableId")) return sourceValue;

        var lookupTableId = config["LookupTableId"]?.ToString();
        if (string.IsNullOrEmpty(lookupTableId)) return sourceValue;

        var lookupTable = await _lookupRepository.GetByIdAsync(lookupTableId);
        if (lookupTable?.Mappings == null)
        {
            _logger.LogWarning($"Lookup table not found: {lookupTableId}");
            return sourceValue;
        }

        // Parse mappings JSON.
        var rawMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(lookupTable.Mappings);
        if (rawMappings == null) return sourceValue;

        var comparer = lookupTable.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var mappings = new Dictionary<string, string>(rawMappings, comparer);
        var key = sourceValue.ToString() ?? string.Empty;

        return mappings.TryGetValue(key, out var value)
            ? value
            : lookupTable.DefaultValue ?? sourceValue;
    }
}
