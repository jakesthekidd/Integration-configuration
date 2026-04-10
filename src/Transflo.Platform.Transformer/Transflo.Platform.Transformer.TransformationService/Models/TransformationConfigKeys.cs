namespace Transflo.Platform.Transformer.TransformationService.Models;

/// <summary>
/// Configuration key constants used in <c>TransformationConfig</c> JSON for each strategy.
/// Eliminates magic strings across all transformation strategy implementations.
/// </summary>
public static class TransformationConfigKeys
{
    public static class Concat
    {
        public const string Fields = "Fields";
        public const string Separator = "Separator";
        public const string SkipEmpty = "SkipEmpty";

        public const string DefaultSeparator = " ";
    }

    public static class DateFormat
    {
        public const string DateInputFormat = "DateInputFormat";
        public const string DateOutputFormat = "DateOutputFormat";

        /// <summary>Round-trip ISO 8601 format used when no output format is specified.</summary>
        public const string DefaultOutputFormat = "o";

        /// <summary>Timezone offset suffix used in fallback format substitution.</summary>
        public const string TimezoneOffsetLong = "zzz";
        public const string TimezoneOffsetShort = "zz";
    }

    public static class Lookup
    {
        public const string LookupTableId = "LookupTableId";
    }

    public static class ArrayFlatten
    {
        public const string SourceArrayPath = "SourceArrayPath";
        public const string ItemField = "ItemField";
        public const string FilterEmpty = "FilterEmpty";
    }

    public static class Substring
    {
        public const string Start = "Start";
        public const string Length = "Length";
    }

    public static class Template
    {
        public const string TemplateKey = "Template";
    }

    public static class Math
    {
        public const string Operation = "Operation";
        public const string Operand = "Operand";
        public const string Precision = "Precision";
    }

    public static class Conditional
    {
        public const string Conditions = "Conditions";
        public const string ConditionLogic = "ConditionLogic";
        public const string ConditionGroups = "ConditionGroups";
        public const string GroupLogic = "GroupLogic";
        public const string Logic = "Logic";
        public const string Field = "Field";
        public const string Operator = "Operator";
        public const string Value = "Value";
        public const string MapSourceOnTrue = "MapSourceOnTrue";
        public const string TrueValue = "TrueValue";
        public const string TruePath = "TruePath";
        public const string FalseValue = "FalseValue";
        public const string FalsePath = "FalsePath";

        public const string DefaultLogic = "AND";
    }

    public static class ConditionalDateFormat
    {
        /// <summary>Source path of the field whose value drives branch selection (e.g. <c>stopType</c>).</summary>
        public const string ConditionField = "ConditionField";
        /// <summary>Array of branch objects, each with a <c>Value</c> and <c>SourcePaths</c>.</summary>
        public const string Branches = "Branches";
        /// <summary>The condition value to match for this branch (case-insensitive).</summary>
        public const string Value = "Value";
        /// <summary>Ordered list of source paths to try; first non-null non-empty value wins.</summary>
        public const string SourcePaths = "SourcePaths";
        /// <summary>.NET format string applied to the UTC result. Defaults to ISO microseconds.</summary>
        public const string OutputFormat = "OutputFormat";

        /// <summary>Default output: ISO 8601 UTC with microsecond precision (e.g. <c>2024-03-15T10:30:00.000000Z</c>).</summary>
        public const string DefaultOutputFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";
    }

    public static class PrefixMap
    {
        /// <summary>Array of field names to map each split part into (e.g. <c>["firstName","lastName"]</c>).</summary>
        public const string Fields = "Fields";
        /// <summary>String to split the source value on. Defaults to a single space.</summary>
        public const string Separator = "Separator";
        /// <summary>When <c>"true"</c>, source entries whose value is null or whitespace are omitted from the result array.</summary>
        public const string SkipEmpty = "SkipEmpty";

        public const string DefaultSeparator = " ";
    }

    /// <summary>Wildcard token used in array source paths (e.g. <c>items[*].field</c>).</summary>
    public const string ArrayWildcard = "[*]";

    /// <summary>Boolean true string value used in config comparisons.</summary>
    public const string BoolTrue = "true";
}
