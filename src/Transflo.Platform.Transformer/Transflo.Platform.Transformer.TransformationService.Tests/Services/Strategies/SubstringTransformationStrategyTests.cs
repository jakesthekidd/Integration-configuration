using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class SubstringTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly SubstringTransformationStrategy _sut;

    public SubstringTransformationStrategyTests()
    {
        _sut = new SubstringTransformationStrategy(_jsonParserMock.Object);
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
    public void TransformationType_Returns_Substring()
    {
        Assert.Equal(TransformationType.Substring, _sut.TransformationType);
    }

    // ── Basic extraction ──────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ExtractsFromStart_WithLength()
    {
        SetupField("code", "ABCDEFGH");
        var ctx = MakeContext("code", """{"Start":0,"Length":3}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("ABC", result);
    }

    [Fact]
    public async Task ApplyAsync_ExtractsFromMidpoint_WithLength()
    {
        SetupField("code", "ABCDEFGH");
        var ctx = MakeContext("code", """{"Start":2,"Length":4}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("CDEF", result);
    }

    [Fact]
    public async Task ApplyAsync_ExtractsToEnd_WhenLengthAbsent()
    {
        SetupField("reference", "REF-001-SUFFIX");
        var ctx = MakeContext("reference", """{"Start":4}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("001-SUFFIX", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFullString_WhenStartIsZeroAndLengthAbsent()
    {
        SetupField("code", "HELLO");
        var ctx = MakeContext("code", """{"Start":0}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("HELLO", result);
    }

    // ── Real-world use cases ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ExtractsYearFromDate()
    {
        // "2024-03-15" → "2024"
        SetupField("pickup_date", "2024-03-15");
        var ctx = MakeContext("pickup_date", """{"Start":0,"Length":4}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024", result);
    }

    [Fact]
    public async Task ApplyAsync_ExtractsCarrierPrefix_FromProNumber()
    {
        // "FXFE123456" → "FXFE"
        SetupField("pro_number", "FXFE123456");
        var ctx = MakeContext("pro_number", """{"Start":0,"Length":4}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("FXFE", result);
    }

    [Fact]
    public async Task ApplyAsync_StripsLeadingPrefix_FromOrderId()
    {
        // "ORD-98765" → "98765"
        SetupField("order_id", "ORD-98765");
        var ctx = MakeContext("order_id", """{"Start":4}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("98765", result);
    }

    // ── Boundary clamping ─────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ClampsStart_WhenStartExceedsStringLength()
    {
        SetupField("code", "ABC");
        var ctx = MakeContext("code", """{"Start":100}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ApplyAsync_ClampsLength_WhenLengthExceedsRemainingCharacters()
    {
        SetupField("code", "ABCDE");
        var ctx = MakeContext("code", """{"Start":3,"Length":999}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("DE", result);
    }

    [Fact]
    public async Task ApplyAsync_ClampsNegativeStart_ToZero()
    {
        SetupField("code", "ABCDE");
        var ctx = MakeContext("code", """{"Start":-5,"Length":3}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("ABC", result);
    }

    // ── Null / invalid config ─────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsOriginalValue_WhenConfigIsNull()
    {
        SetupField("code", "ABCDE");
        var ctx = MakeContext("code", null);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("ABCDE", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsOriginalValue_WhenStartKeyMissing()
    {
        SetupField("code", "ABCDE");
        var ctx = MakeContext("code", """{"Length":3}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("ABCDE", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsOriginalValue_WhenConfigIsInvalidJson()
    {
        SetupField("code", "ABCDE");
        var ctx = MakeContext("code", "not-json");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("ABCDE", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenSourceFieldIsNull()
    {
        SetupField("code", null);
        var ctx = MakeContext("code", """{"Start":0,"Length":3}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }
}
