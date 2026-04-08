namespace Transflo.Platform.Transformer.TransformationService.Models;

/// <summary>
/// String constants for the <c>Operation</c> field in Math transformation config.
/// All values are lowercase; the strategy compares against lower-invariant input.
/// </summary>
public static class MathOperations
{
    // ── Binary operations ─────────────────────────────────────────────────────

    /// <summary>result = value + Operand</summary>
    public const string Add      = "add";

    /// <summary>result = value − Operand</summary>
    public const string Subtract = "subtract";

    /// <summary>result = value × Operand (returns original when Operand is absent)</summary>
    public const string Multiply = "multiply";

    /// <summary>result = value ÷ Operand (returns original when Operand is 0)</summary>
    public const string Divide   = "divide";

    /// <summary>result = value % Operand (returns original when Operand is 0)</summary>
    public const string Mod      = "mod";

    // ── Unary operations ──────────────────────────────────────────────────────

    /// <summary>result = |value|</summary>
    public const string Abs   = "abs";

    /// <summary>result = ceiling of value</summary>
    public const string Ceil  = "ceil";

    /// <summary>result = floor of value</summary>
    public const string Floor = "floor";

    /// <summary>result = rounded value (AwayFromZero; Precision applied if supplied)</summary>
    public const string Round = "round";
}
