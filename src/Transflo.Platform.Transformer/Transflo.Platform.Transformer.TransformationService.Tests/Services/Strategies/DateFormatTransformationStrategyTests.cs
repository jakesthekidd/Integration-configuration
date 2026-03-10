using Microsoft.Extensions.Logging;
using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class DateFormatTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly Mock<ILogger<DateFormatTransformationStrategy>> _loggerMock = new();
    private readonly DateFormatTransformationStrategy _sut;

    public DateFormatTransformationStrategyTests()
    {
        _sut = new DateFormatTransformationStrategy(_jsonParserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void TransformationType_Returns_DateFormat()
    {
        Assert.Equal(TransformationType.DateFormat, _sut.TransformationType);
    }

    [Fact]
    public async Task ApplyAsync_ParsesMcLeodPickupDate_ToIso8601()
    {
        // fm-mcleod-003: pickup_date in McLeod yyyyMMddHHmmsszzz format → ISO 8601 UTC
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "pickup_date",
            TargetPath = "pickupDate",
            TransformationConfig = """{"DateInputFormat":"yyyyMMddHHmmsszzz","DateOutputFormat":"o"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "pickup_date"))
            .ReturnsAsync("20260116000000-08:00");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("2026-01-16T08:00:00.0000000Z", result);
    }

    [Theory]
    [InlineData("20260116000000-08:00", "yyyyMMddHHmmsszzz", "o", "2026-01-16T08:00:00.0000000Z")]
    [InlineData("20260116000000-08:00", "yyyyMMddHHmmsszzz", "yyyy-MM-dd", "2026-01-16")]
    [InlineData("2026-01-16T00:00:00Z", null, "yyyy-MM-dd", "2026-01-16")]
    [InlineData("2024-01-15T12:30:00Z", null, "MM/dd/yyyy", "01/15/2024")]
    [InlineData("2024-01-15T12:30:00Z", null, "dd-MMM-yyyy", "15-Jan-2024")]
    public void ApplyDateFormat_FormatsToOutputFormat(string input, string? inputFormat, string outputFormat, string expected)
    {
        var config = new Dictionary<string, object> { ["DateOutputFormat"] = outputFormat };
        if (inputFormat != null)
            config["DateInputFormat"] = inputFormat;

        var result = _sut.ApplyDateFormat(input, config);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ApplyDateFormat_UsesISO8601_WhenNoOutputFormatSpecified()
    {
        var result = _sut.ApplyDateFormat("2026-01-16T00:00:00Z", null);
        Assert.NotNull(result);
        Assert.Contains("2026-01-16", result!.ToString());
    }

    [Fact]
    public void ApplyDateFormat_ParsesWithExplicitInputFormat()
    {
        // fm-mcleod-003: explicit DateInputFormat for McLeod compact date with offset
        var config = new Dictionary<string, object>
        {
            ["DateInputFormat"] = "yyyyMMddHHmmsszzz",
            ["DateOutputFormat"] = "yyyy-MM-dd"
        };
        var result = _sut.ApplyDateFormat("20260116000000-08:00", config);
        Assert.Equal("2026-01-16", result);
    }

    [Fact]
    public void ApplyDateFormat_ReturnsOriginalInput_WhenDateCannotBeParsed()
    {
        var config = new Dictionary<string, object> { ["DateOutputFormat"] = "yyyy-MM-dd" };
        var result = _sut.ApplyDateFormat("not-a-date", config);
        Assert.Equal("not-a-date", result);
    }

    [Fact]
    public void ApplyDateFormat_FallsBackToTryParse_WhenExplicitFormatDoesNotMatch()
    {
        // When the explicit McLeod format doesn't match but the input is a valid ISO 8601
        // date, the TryParse fallback succeeds and the result is formatted normally
        var config = new Dictionary<string, object>
        {
            ["DateInputFormat"] = "yyyyMMddHHmmsszzz",
            ["DateOutputFormat"] = "yyyy-MM-dd"
        };
        var result = _sut.ApplyDateFormat("2026-01-16T00:00:00Z", config);
        Assert.Equal("2026-01-16", result);
    }

    [Fact]
    public void ApplyDateFormat_ReturnsNull_WhenInputIsNull()
    {
        var result = _sut.ApplyDateFormat(null, null);
        Assert.Null(result);
    }

    [Fact]
    public void ApplyDateFormat_ReturnsNull_WhenInputIsWhitespace()
    {
        var result = _sut.ApplyDateFormat("   ", null);
        Assert.Null(result);
    }
}
