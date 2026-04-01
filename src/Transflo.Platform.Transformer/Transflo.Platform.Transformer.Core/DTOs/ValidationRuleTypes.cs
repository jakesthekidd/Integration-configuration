namespace Transflo.Platform.Transformer.Core.DTOs;

/// <summary>
/// Lowercase canonical names for ValidationRules rule types.
/// Comparison is always done case-insensitively via ToLowerInvariant().
/// </summary>
public static class ValidationRuleTypes
{
    public const string Required = "required";
    public const string Length = "length";
    public const string Range = "range";
    public const string Enum = "enum";
    public const string Date = "date";
    public const string Regex = "regex";
    public const string Pattern = "pattern"; // alias for Regex
}
