namespace Transflo.Platform.Transformer.Core.DTOs;

public static class ValidationCodes
{
    public const string VersionNotFound = "VERSION_NOT_FOUND";
    public const string MissingTargetPath = "MISSING_TARGET_PATH";
    public const string MissingSourcePath = "MISSING_SOURCE_PATH";
    public const string InvalidTransformationType = "INVALID_TRANSFORMATION_TYPE";
    public const string InvalidConfigJson = "INVALID_CONFIG_JSON";
    public const string MissingConfigFields = "MISSING_CONFIG_FIELDS";
    public const string InvalidValidationRulesJson = "INVALID_VALIDATION_RULES_JSON";
    public const string InvalidRegexPattern = "INVALID_REGEX_PATTERN";
    public const string DuplicateTargetPath = "DUPLICATE_TARGET_PATH";
    public const string UnknownRuleType = "UNKNOWN_RULE_TYPE";
    public const string MissingRuleProperty = "MISSING_RULE_PROPERTY";
    public const string InvalidRulePropertyValue = "INVALID_RULE_PROPERTY_VALUE";
    public const string FieldRequired = "FIELD_REQUIRED";
    public const string ValueViolatesRule = "VALUE_VIOLATES_RULE";
}
