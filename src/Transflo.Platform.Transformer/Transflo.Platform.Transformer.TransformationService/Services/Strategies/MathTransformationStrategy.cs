using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

/// <summary>
/// Performs an arithmetic or rounding operation on a numeric source field.
///
/// <b>TransformationConfig schema</b>
/// <code>
/// {
///   "Operation": "multiply",  // required — see supported operations below
///   "Operand":   1.609344,    // required for binary operations
///   "Precision": 2            // optional — rounds the final result to N decimal places
/// }
/// </code>
///
/// <b>Supported operations</b> (case-insensitive):
/// <list type="bullet">
///   <item><b>add</b>      — result = value + Operand</item>
///   <item><b>subtract</b> — result = value − Operand</item>
///   <item><b>multiply</b> — result = value × Operand</item>
///   <item><b>divide</b>   — result = value ÷ Operand  (returns original if Operand is 0)</item>
///   <item><b>mod</b>      — result = value % Operand  (returns original if Operand is 0)</item>
///   <item><b>abs</b>      — result = |value|</item>
///   <item><b>ceil</b>     — result = ceiling of value</item>
///   <item><b>floor</b>    — result = floor of value</item>
///   <item><b>round</b>    — result = rounded value (uses Precision when supplied)</item>
/// </list>
///
/// <c>Precision</c> is applied as a post-processing step to any operation, not only <c>round</c>.
/// Returns the original source value unchanged when it cannot be parsed as a number.
/// </summary>
public class MathTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;

    public MathTransformationStrategy(IJsonParserService jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public TransformationType TransformationType => TransformationType.Math;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var raw = await _jsonParser.GetValueAtPathAsync(context.SourceData, context.Mapping.SourcePath);
        var strValue = raw is JsonElement je ? je.GetString() : raw?.ToString();

        if (!double.TryParse(strValue,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var number))
        {
            return raw; // non-numeric — return as-is
        }

        var config = ParseConfig(context.Mapping.TransformationConfig);
        if (config is null
            || !config.TryGetValue(TransformationConfigKeys.Math.Operation, out var opRaw)
            || opRaw?.ToString() is not { Length: > 0 } operationStr)
        {
            return raw;
        }

        var operation = operationStr.ToLowerInvariant();

        double operand = 0;
        bool hasOperand = config.TryGetValue(TransformationConfigKeys.Math.Operand, out var operandRaw)
            && operandRaw is JsonElement operandEl
            && operandEl.TryGetDouble(out operand);

        double result = operation switch
        {
            MathOperations.Add => number + (hasOperand ? operand : 0),
            MathOperations.Subtract => number - (hasOperand ? operand : 0),
            MathOperations.Multiply => hasOperand ? number * operand : number,
            MathOperations.Divide => hasOperand && operand != 0 ? number / operand : number,
            MathOperations.Mod => hasOperand && operand != 0 ? number % operand : number,
            MathOperations.Abs => Math.Abs(number),
            MathOperations.Ceil => Math.Ceiling(number),
            MathOperations.Floor => Math.Floor(number),
            MathOperations.Round => Math.Round(number, MidpointRounding.AwayFromZero),
            _ => number
        };

        if (config.TryGetValue(TransformationConfigKeys.Math.Precision, out var precRaw)
            && precRaw is JsonElement precEl
            && precEl.TryGetInt32(out var precision)
            && precision >= 0)
        {
            result = Math.Round(result, precision, MidpointRounding.AwayFromZero);
        }

        // Return as a whole number when there is no fractional part
        return result == Math.Floor(result) && Math.Abs(result) < 1e15
            ? (object)(long)result
            : result;
    }

    private static Dictionary<string, object>? ParseConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return null;
        }
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(configJson); }
        catch { return null; }
    }
}
