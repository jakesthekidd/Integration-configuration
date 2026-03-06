using Moq;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

namespace Transflo.Platform.Transformer.Core.Tests.Services.Transformations;

public class ConcatTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly ConcatTransformationStrategy _sut;

    public ConcatTransformationStrategyTests()
    {
        _sut = new ConcatTransformationStrategy(_jsonParserMock.Object);
    }

    [Fact]
    public void TransformationType_Returns_Concat()
    {
        Assert.Equal(TransformationType.Concat, _sut.TransformationType);
    }

    [Fact]
    public async Task ApplyAsync_ConcatenatesFields_WithDefaultSeparator()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "fullName",
            TransformationConfig = """{"Fields":["firstName","lastName"]}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "firstName")).ReturnsAsync("John");
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "lastName")).ReturnsAsync("Doe");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("John Doe", result);
    }

    [Fact]
    public async Task ApplyAsync_ConcatenatesFields_WithCustomSeparator()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "fullName",
            TransformationConfig = """{"Fields":["firstName","lastName"],"Separator":", "}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "firstName")).ReturnsAsync("Doe");
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "lastName")).ReturnsAsync("John");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("Doe, John", result);
    }

    [Fact]
    public async Task ApplyAsync_SkipsEmptyValues_WhenSkipEmptyIsTrue()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "address",
            TransformationConfig = """{"Fields":["line1","line2","city"],"Separator":", ","SkipEmpty":"true"}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "line1")).ReturnsAsync("123 Main St");
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "line2")).ReturnsAsync((object?)null);
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "city")).ReturnsAsync("Springfield");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("123 Main St, Springfield", result);
    }

    [Fact]
    public async Task ApplyAsync_IncludesEmptyValues_WhenSkipEmptyIsFalse()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "code",
            TransformationConfig = """{"Fields":["part1","part2","part3"],"Separator":"-","SkipEmpty":"false"}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "part1")).ReturnsAsync("A");
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "part2")).ReturnsAsync((object?)null);
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "part3")).ReturnsAsync("C");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        // null values are not added, only non-null ones
        Assert.Equal("A-C", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConfigIsNull()
    {
        var mapping = new FieldMapping { SourcePath = "", TargetPath = "fullName", TransformationConfig = null };
        var result = await _sut.ApplyAsync(new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = mapping
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenFieldsKeyMissing()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "fullName",
            TransformationConfig = """{"Separator":" "}"""
        };
        var result = await _sut.ApplyAsync(new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = mapping
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsEmptyString_WhenAllFieldsNull_AndSkipEmptyFalse()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "fullName",
            TransformationConfig = """{"Fields":["firstName","lastName"]}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "firstName")).ReturnsAsync((object?)null);
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "lastName")).ReturnsAsync((object?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal(string.Empty, result);
    }
}
