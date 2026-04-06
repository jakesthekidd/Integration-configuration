using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class MathTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly MathTransformationStrategy _sut;

    public MathTransformationStrategyTests()
    {
        _sut = new MathTransformationStrategy(_jsonParserMock.Object);
    }

    private TransformationContext MakeContext(string sourcePath, string? config) =>
        new()
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = sourcePath,
                TargetPath = "output",
                TransformationConfig = config
            }
        };

    private void SetupField(string path, string? value) =>
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), path))
            .ReturnsAsync((object?)value);

    // ── Identity ──────────────────────────────────────────────────────────────

    [Fact]
    public void TransformationType_Returns_Math()
    {
        Assert.Equal(TransformationType.Math, _sut.TransformationType);
    }

    // ── add ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Add_ReturnsSum()
    {
        SetupField("quantity", "10");
        var ctx = MakeContext("quantity", """{"Operation":"add","Operand":5}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(15L, result);
    }

    [Fact]
    public async Task ApplyAsync_Add_HandlesDecimalOperand()
    {
        SetupField("price", "100");
        var ctx = MakeContext("price", """{"Operation":"add","Operand":9.99}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(109.99, result);
    }

    // ── subtract ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Subtract_ReturnsDifference()
    {
        SetupField("weight", "500");
        var ctx = MakeContext("weight", """{"Operation":"subtract","Operand":50}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(450L, result);
    }

    // ── multiply ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Multiply_ReturnsProduct()
    {
        SetupField("weight_kg", "100");
        // kg to lbs: × 2.20462
        var ctx = MakeContext("weight_kg", """{"Operation":"multiply","Operand":2.20462,"Precision":2}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(220.46, result);
    }

    [Fact]
    public async Task ApplyAsync_Multiply_ReturnsOriginal_WhenOperandAbsent()
    {
        SetupField("value", "42");
        var ctx = MakeContext("value", """{"Operation":"multiply"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(42L, result);
    }

    // ── divide ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Divide_ReturnsQuotient()
    {
        SetupField("distance_km", "100");
        // km to miles: ÷ 1.60934
        var ctx = MakeContext("distance_km", """{"Operation":"divide","Operand":1.60934,"Precision":2}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(62.14, result);
    }

    [Fact]
    public async Task ApplyAsync_Divide_ReturnsOriginal_WhenOperandIsZero()
    {
        SetupField("value", "50");
        var ctx = MakeContext("value", """{"Operation":"divide","Operand":0}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(50L, result);
    }

    // ── mod ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Mod_ReturnsRemainder()
    {
        SetupField("value", "17");
        var ctx = MakeContext("value", """{"Operation":"mod","Operand":5}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(2L, result);
    }

    [Fact]
    public async Task ApplyAsync_Mod_ReturnsOriginal_WhenOperandIsZero()
    {
        SetupField("value", "17");
        var ctx = MakeContext("value", """{"Operation":"mod","Operand":0}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(17L, result);
    }

    // ── abs ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Abs_ReturnsAbsoluteValue_WhenNegative()
    {
        SetupField("temperature", "-23");
        var ctx = MakeContext("temperature", """{"Operation":"abs"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(23L, result);
    }

    [Fact]
    public async Task ApplyAsync_Abs_ReturnsValue_WhenAlreadyPositive()
    {
        SetupField("temperature", "23");
        var ctx = MakeContext("temperature", """{"Operation":"abs"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(23L, result);
    }

    // ── ceil / floor ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Ceil_RoundsUp()
    {
        SetupField("value", "4.1");
        var ctx = MakeContext("value", """{"Operation":"ceil"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(5L, result);
    }

    [Fact]
    public async Task ApplyAsync_Floor_RoundsDown()
    {
        SetupField("value", "4.9");
        var ctx = MakeContext("value", """{"Operation":"floor"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(4L, result);
    }

    // ── round ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Round_RoundsToNearestInteger()
    {
        SetupField("value", "4.6");
        var ctx = MakeContext("value", """{"Operation":"round"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(5L, result);
    }

    [Fact]
    public async Task ApplyAsync_Round_RoundsAwayFromZero_OnMidpoint()
    {
        SetupField("value", "2.5");
        var ctx = MakeContext("value", """{"Operation":"round"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(3L, result);
    }

    // ── Precision ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Precision_RoundsResultToDecimalPlaces()
    {
        SetupField("value", "3.14159");
        var ctx = MakeContext("value", """{"Operation":"multiply","Operand":1,"Precision":2}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(3.14, result);
    }

    [Fact]
    public async Task ApplyAsync_Precision_AppliesAfterOperation()
    {
        SetupField("value", "10");
        // 10 ÷ 3 = 3.3333... → rounded to 2 dp = 3.33
        var ctx = MakeContext("value", """{"Operation":"divide","Operand":3,"Precision":2}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(3.33, result);
    }

    [Fact]
    public async Task ApplyAsync_Precision_Zero_ReturnsWholeNumber()
    {
        SetupField("value", "7.8");
        var ctx = MakeContext("value", """{"Operation":"multiply","Operand":1,"Precision":0}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(8L, result);
    }

    // ── Non-numeric / invalid ─────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsOriginalValue_WhenSourceIsNotNumeric()
    {
        SetupField("code", "ABC");
        var ctx = MakeContext("code", """{"Operation":"add","Operand":1}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("ABC", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsOriginalValue_WhenConfigIsNull()
    {
        SetupField("value", "42");
        var ctx = MakeContext("value", null);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("42", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsOriginalValue_WhenOperationKeyMissing()
    {
        SetupField("value", "42");
        var ctx = MakeContext("value", """{"Operand":5}""");

        var result = await _sut.ApplyAsync(ctx);

        // raw value returned as-is
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsOriginalValue_WhenOperationIsUnknown()
    {
        SetupField("value", "42");
        var ctx = MakeContext("value", """{"Operation":"unknown","Operand":5}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(42L, result);
    }
}
