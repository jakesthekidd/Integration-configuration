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
    public async Task ApplyAsync_CallsParser_ThenFormatsDate()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "createdAt",
            TargetPath = "created",
            TransformationConfig = """{"DateOutputFormat":"yyyy-MM-dd"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "createdAt"))
            .ReturnsAsync("2024-03-15T10:00:00Z");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("2024-03-15", result);
    }

    [Theory]
    [InlineData("2024-01-15T12:30:00Z", "o", "2024-01-15T12:30:00.0000000Z")]
    [InlineData("2024-01-15T12:30:00Z", "yyyy-MM-dd", "2024-01-15")]
    [InlineData("2024-01-15T12:30:00Z", "MM/dd/yyyy", "01/15/2024")]
    [InlineData("2024-01-15T12:30:00Z", "dd-MMM-yyyy", "15-Jan-2024")]
    public void ApplyDateFormat_FormatsToOutputFormat(string input, string outputFormat, string expected)
    {
        var config = new Dictionary<string, object> { ["DateOutputFormat"] = outputFormat };
        var result = _sut.ApplyDateFormat(input, config);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ApplyDateFormat_UsesISO8601_WhenNoOutputFormatSpecified()
    {
        var result = _sut.ApplyDateFormat("2024-06-01T00:00:00Z", null);
        Assert.NotNull(result);
        Assert.Contains("2024-06-01", result!.ToString());
    }

    [Fact]
    public void ApplyDateFormat_ParsesWithExplicitInputFormat()
    {
        var config = new Dictionary<string, object>
        {
            ["DateInputFormat"] = "dd/MM/yyyy",
            ["DateOutputFormat"] = "yyyy-MM-dd"
        };
        var result = _sut.ApplyDateFormat("15/03/2024", config);
        Assert.Equal("2024-03-15", result);
    }

    [Fact]
    public void ApplyDateFormat_ReturnsOriginalInput_WhenDateCannotBeParsed()
    {
        var config = new Dictionary<string, object> { ["DateOutputFormat"] = "yyyy-MM-dd" };
        var result = _sut.ApplyDateFormat("not-a-date", config);
        Assert.Equal("not-a-date", result);
    }

    [Fact]
    public void ApplyDateFormat_ReturnsOriginalInput_WhenExplicitFormatMismatch()
    {
        var config = new Dictionary<string, object>
        {
            ["DateInputFormat"] = "dd/MM/yyyy",
            ["DateOutputFormat"] = "yyyy-MM-dd"
        };
        var result = _sut.ApplyDateFormat("2024-03-15", config);
        Assert.Equal("2024-03-15", result);
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
