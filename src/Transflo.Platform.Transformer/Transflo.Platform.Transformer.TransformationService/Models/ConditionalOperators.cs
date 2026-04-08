namespace Transflo.Platform.Transformer.TransformationService.Models;

/// <summary>
/// String constants for the <c>Operator</c> field in Conditional transformation conditions.
/// All values are lowercase; the strategy compares against lower-invariant input.
/// </summary>
public static class ConditionalOperators
{
    // ── Equality ──────────────────────────────────────────────────────────────

    public const string Equals    = "equals";
    public const string Eq        = "eq";
    public const string NotEquals = "notequals";
    public const string Ne        = "ne";

    // ── String matching ───────────────────────────────────────────────────────

    public const string Contains   = "contains";
    public const string StartsWith  = "startswith";
    public const string EndsWith    = "endswith";

    // ── Numeric comparison ────────────────────────────────────────────────────

    public const string GreaterThan           = "greaterthan";
    public const string Gt                    = "gt";
    public const string LessThan              = "lessthan";
    public const string Lt                    = "lt";
    public const string GreaterThanOrEquals   = "greaterthanorequals";
    public const string Gte                   = "gte";
    public const string LessThanOrEquals      = "lessthanorequals";
    public const string Lte                   = "lte";

    // ── Existence ─────────────────────────────────────────────────────────────

    public const string IsEmpty    = "isempty";
    public const string IsNotEmpty = "isnotempty";

    // ── Set membership ────────────────────────────────────────────────────────

    /// <summary>Value is a comma-separated list; passes when field value is in the list.</summary>
    public const string In    = "in";
    /// <summary>Value is a comma-separated list; passes when field value is NOT in the list.</summary>
    public const string NotIn = "notin";

    // ── Logic values ──────────────────────────────────────────────────────────

    public const string And = "AND";
    public const string Or  = "OR";
}
