using System.Text.Json;
using System.Text.RegularExpressions;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using TransformationType = Transflo.Platform.Transformer.TransformationService.Models.TransformationType;

namespace Transflo.Platform.Transformer.Core.Services;

public class FieldMappingValidationService : IFieldMappingValidationService
{
    private readonly ITemplateVersionRepository _versionRepo;
    private readonly IFieldMappingRepository _mappingRepo;

    public FieldMappingValidationService(
        ITemplateVersionRepository versionRepo,
        IFieldMappingRepository mappingRepo)
    {
        _versionRepo = versionRepo;
        _mappingRepo = mappingRepo;
    }

    public async Task<MappingValidationResult> ValidateAsync(Guid templateId, int version)
    {
        var templateVersion = await _versionRepo.GetByVersionAsync(templateId, version);
        if (templateVersion is null)
        {
            return new MappingValidationResult
            {
                IsValid = false,
                Issues = [new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Code = ValidationCodes.VersionNotFound,
                    Message = $"Template version {version} not found for template {templateId}."
                }]
            };
        }

        var mappings = await _mappingRepo.GetByTemplateVersionIdOrderedAsync(templateVersion.Id);
        var issues = new List<ValidationIssue>();

        // Per-mapping rules
        for (int i = 0; i < mappings.Count; i++)
        {
            var fieldMapping = mappings[i];
            int displayIndex = i + 1;

            ValidateRequiredFields(fieldMapping, displayIndex, issues);
            ValidateTransformationType(fieldMapping, displayIndex, issues);
            ValidateTransformationConfig(fieldMapping, displayIndex, issues);
            ValidateValidationRules(fieldMapping, displayIndex, issues);
        }

        // Cross-mapping rules
        ValidateDuplicateTargetPaths(mappings, issues);

        return new MappingValidationResult
        {
            IsValid = !issues.Any(i => i.Severity == ValidationSeverity.Error),
            Issues = issues
        };
    }

    // ── Per-mapping validators ────────────────────────────────────────────────

    private static void ValidateRequiredFields(FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(fieldMapping.TargetPath))
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.MissingTargetPath,
                Message = "TargetPath is required for all mappings.",
                MappingIndex = index
            });
        }

        // Constant type derives its value from config, not a source path
        if (fieldMapping.TransformationType != TransformationType.Constant &&
            string.IsNullOrWhiteSpace(fieldMapping.SourcePath))
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.MissingSourcePath,
                Message = $"SourcePath is required for transformation type '{fieldMapping.TransformationType}'.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }
    }

    private static void ValidateTransformationType(FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        if (!Enum.IsDefined(typeof(TransformationType), fieldMapping.TransformationType))
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidTransformationType,
                Message = $"Transformation type '{fieldMapping.TransformationType}' is not valid.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }
    }

    private static void ValidateTransformationConfig(FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        Dictionary<string, object>? config = null;

        if (!string.IsNullOrWhiteSpace(fieldMapping.TransformationConfig))
        {
            try
            {
                config = JsonSerializer.Deserialize<Dictionary<string, object>>(fieldMapping.TransformationConfig);
            }
            catch
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Code = ValidationCodes.InvalidConfigJson,
                    Message = "TransformationConfig is not valid JSON.",
                    MappingIndex = index,
                    TargetPath = fieldMapping.TargetPath
                });
                return;
            }
        }

        switch (fieldMapping.TransformationType)
        {
            case TransformationType.Concat:
                if (config is null || !config.ContainsKey("Fields"))
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Code = ValidationCodes.MissingConfigFields,
                        Message = "Concat transformation requires 'Fields' in TransformationConfig.",
                        MappingIndex = index,
                        TargetPath = fieldMapping.TargetPath
                    });
                }
                break;

            case TransformationType.Lookup:
                var hasLookupId = config is not null &&
                                  config.TryGetValue("LookupTableId", out var lid) &&
                                  Guid.TryParse(lid?.ToString(), out _);
                if (!hasLookupId)
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Code = ValidationCodes.MissingConfigFields,
                        Message = "Lookup transformation requires a valid 'LookupTableId' GUID in TransformationConfig.",
                        MappingIndex = index,
                        TargetPath = fieldMapping.TargetPath
                    });
                }
                break;
        }
    }

    private static void ValidateValidationRules(FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(fieldMapping.ValidationRules))
        {
            return;
        }

        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(fieldMapping.ValidationRules);
        }
        catch
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidValidationRulesJson,
                Message = "ValidationRules is not valid JSON.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
            return;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            ValidateArrayStyleRules(root, fieldMapping, index, issues);
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            ValidateLegacyObjectStyleRule(root, fieldMapping, index, issues);
        }
    }

    // ── Array-style rules: [{"Type":"Required",...}, ...] ────────────────────

    private static void ValidateArrayStyleRules(JsonElement array, FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        int ruleIndex = 0;
        foreach (var rule in array.EnumerateArray())
        {
            ruleIndex++;

            if (rule.ValueKind != JsonValueKind.Object)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Code = ValidationCodes.InvalidValidationRulesJson,
                    Message = $"ValidationRules entry {ruleIndex} must be a JSON object.",
                    MappingIndex = index,
                    TargetPath = fieldMapping.TargetPath
                });
                continue;
            }

            if (!TryGetPropertyIgnoreCase(rule, "Type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Code = ValidationCodes.MissingRuleProperty,
                    Message = $"ValidationRules entry {ruleIndex} is missing the required 'Type' property.",
                    MappingIndex = index,
                    TargetPath = fieldMapping.TargetPath
                });
                continue;
            }

            switch (typeEl.GetString()!.ToLowerInvariant())
            {
                case ValidationRuleTypes.Required:
                    break;
                case ValidationRuleTypes.Length:
                    ValidateLengthRule(rule, ruleIndex, fieldMapping, index, issues);
                    break;
                case ValidationRuleTypes.Range:
                    ValidateRangeRule(rule, ruleIndex, fieldMapping, index, issues);
                    break;
                case ValidationRuleTypes.Enum:
                    ValidateEnumRule(rule, ruleIndex, fieldMapping, index, issues);
                    break;
                case ValidationRuleTypes.Date:
                    ValidateDateRule(rule, ruleIndex, fieldMapping, index, issues);
                    break;
                case ValidationRuleTypes.Regex:
                case ValidationRuleTypes.Pattern:
                    ValidateRegexRule(rule, ruleIndex, fieldMapping, index, issues);
                    break;
                default:
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Code = ValidationCodes.UnknownRuleType,
                        Message = $"ValidationRules entry {ruleIndex} has unknown type '{typeEl.GetString()}'.",
                        MappingIndex = index,
                        TargetPath = fieldMapping.TargetPath
                    });
                    break;
            }
        }
    }

    private static void ValidateLengthRule(JsonElement rule, int ruleIndex, FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        int minLength = 0, maxLength = 0;
        bool hasMin = TryGetPropertyIgnoreCase(rule, "MinLength", out var minEl) && minEl.TryGetInt32(out minLength);
        bool hasMax = TryGetPropertyIgnoreCase(rule, "MaxLength", out var maxEl) && maxEl.TryGetInt32(out maxLength);

        if (!hasMin && !hasMax)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.MissingRuleProperty,
                Message = $"ValidationRules entry {ruleIndex} (Length) requires at least 'MinLength' or 'MaxLength'.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
            return;
        }

        if (hasMin && minLength < 0)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidRulePropertyValue,
                Message = $"ValidationRules entry {ruleIndex} (Length): 'MinLength' must be 0 or greater.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }

        if (hasMax && maxLength < 1)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidRulePropertyValue,
                Message = $"ValidationRules entry {ruleIndex} (Length): 'MaxLength' must be 1 or greater.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }

        if (hasMin && hasMax && maxLength < minLength)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidRulePropertyValue,
                Message = $"ValidationRules entry {ruleIndex} (Length): 'MaxLength' ({maxLength}) must be greater than or equal to 'MinLength' ({minLength}).",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }
    }

    private static void ValidateRangeRule(JsonElement rule, int ruleIndex, FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        double minValue = 0, maxValue = 0;
        bool hasMin = TryGetPropertyIgnoreCase(rule, "MinValue", out var minEl) && minEl.TryGetDouble(out minValue);
        bool hasMax = TryGetPropertyIgnoreCase(rule, "MaxValue", out var maxEl) && maxEl.TryGetDouble(out maxValue);

        if (!hasMin && !hasMax)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.MissingRuleProperty,
                Message = $"ValidationRules entry {ruleIndex} (Range) requires at least 'MinValue' or 'MaxValue'.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
            return;
        }

        if (hasMin && hasMax && maxValue < minValue)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidRulePropertyValue,
                Message = $"ValidationRules entry {ruleIndex} (Range): 'MaxValue' ({maxValue}) must be greater than or equal to 'MinValue' ({minValue}).",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }
    }

    private static void ValidateEnumRule(JsonElement rule, int ruleIndex, FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        if (!TryGetPropertyIgnoreCase(rule, "AllowedValues", out var allowedEl) || allowedEl.ValueKind != JsonValueKind.Array)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.MissingRuleProperty,
                Message = $"ValidationRules entry {ruleIndex} (Enum) requires 'AllowedValues' as a non-empty array.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
            return;
        }

        if (allowedEl.GetArrayLength() == 0)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidRulePropertyValue,
                Message = $"ValidationRules entry {ruleIndex} (Enum): 'AllowedValues' must not be empty.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }
    }

    private static void ValidateDateRule(JsonElement rule, int ruleIndex, FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        if (!TryGetPropertyIgnoreCase(rule, "Format", out var formatEl))
        {
            return; // Format is optional; absence means any parseable date is accepted
        }

        var format = formatEl.GetString();
        if (string.IsNullOrEmpty(format))
        {
            return;
        }

        try
        {
            _ = DateTime.Now.ToString(format);
        }
        catch
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidRulePropertyValue,
                Message = $"ValidationRules entry {ruleIndex} (Date): 'Format' value '{format}' is not a valid date format string.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }
    }

    private static void ValidateRegexRule(JsonElement rule, int ruleIndex, FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        // Accept either "Pattern" or "Regex" as the property name
        if (!TryGetPropertyIgnoreCase(rule, "Pattern", out var patternEl) &&
            !TryGetPropertyIgnoreCase(rule, "Regex", out patternEl))
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.MissingRuleProperty,
                Message = $"ValidationRules entry {ruleIndex} (Regex) requires a 'Pattern' or 'Regex' property.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
            return;
        }

        var pattern = patternEl.GetString();
        if (string.IsNullOrEmpty(pattern))
        {
            return;
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidRegexPattern,
                Message = $"ValidationRules entry {ruleIndex} (Regex): '{pattern}' is not a valid regex.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }
    }

    // ── Legacy object-style rule: {"pattern": "..."} ─────────────────────────

    private static void ValidateLegacyObjectStyleRule(JsonElement root, FieldMapping fieldMapping, int index, List<ValidationIssue> issues)
    {
        if (!root.TryGetProperty("pattern", out var patternElement))
        {
            return;
        }

        var pattern = patternElement.GetString();
        if (string.IsNullOrEmpty(pattern))
        {
            return;
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.InvalidRegexPattern,
                Message = $"ValidationRules contains an invalid regex pattern: '{pattern}'.",
                MappingIndex = index,
                TargetPath = fieldMapping.TargetPath
            });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    // ── Cross-mapping validators ──────────────────────────────────────────────

    private static void ValidateDuplicateTargetPaths(List<FieldMapping> mappings, List<ValidationIssue> issues)
    {
        var duplicates = mappings
            .Where(m => !string.IsNullOrWhiteSpace(m.TargetPath))
            .GroupBy(m => m.TargetPath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicates)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = ValidationCodes.DuplicateTargetPath,
                Message = $"Target path '{group.Key}' is used by {group.Count()} mappings. Each target path must be unique.",
                TargetPath = group.Key
            });
        }
    }
}
