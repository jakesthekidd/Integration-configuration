using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class ArrayFlattenTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly ArrayFlattenTransformationStrategy _sut;

    public ArrayFlattenTransformationStrategyTests()
    {
        _sut = new ArrayFlattenTransformationStrategy(_jsonParserMock.Object);
    }

    [Fact]
    public void TransformationType_Returns_ArrayFlatten()
    {
        Assert.Equal(TransformationType.ArrayFlatten, _sut.TransformationType);
    }

    [Fact]
    public async Task ApplyAsync_FlattensArrayField_UsingConfig()
    {
        // fm-mcleod-110: collect notes from all stops into a single notes array
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "notes",
            TransformationConfig = """{"SourceArrayPath":"stops","ItemField":"notes","FilterEmpty":true}"""
        };

        var json = """[{"notes":"Call ahead 30 minutes"},{"notes":"Liftgate required"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal("Call ahead 30 minutes", list[0]);
        Assert.Equal("Liftgate required", list[1]);
    }

    [Fact]
    public async Task ApplyAsync_FlattensArrayField_UsingWildcardSourcePath()
    {
        // fm-mcleod-110: stops[*].city wildcard extracts city from each stop
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "stops[*].city",
            TargetPath = "cities"
        };

        var json = """[{"city":"Memphis"},{"city":"Dallas"},{"city":"Atlanta"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal("Memphis", list[0]);
        Assert.Equal("Dallas", list[1]);
        Assert.Equal("Atlanta", list[2]);
    }

    [Fact]
    public async Task ApplyAsync_FiltersEmptyValues_WhenFilterEmptyIsTrue()
    {
        // fm-mcleod-110: stops with empty notes are excluded when FilterEmpty is true
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "stops[*].notes",
            TargetPath = "notes",
            TransformationConfig = """{"FilterEmpty":"true"}"""
        };

        var json = """[{"notes":"Call ahead 30 minutes"},{"notes":""},{"notes":"Liftgate required"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal("Call ahead 30 minutes", list[0]);
        Assert.Equal("Liftgate required", list[1]);
    }

    [Fact]
    public async Task ApplyAsync_IncludesEmptyValues_WhenFilterEmptyIsFalse()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "stops[*].notes",
            TargetPath = "notes",
            TransformationConfig = """{"FilterEmpty":"false"}"""
        };

        var json = """[{"notes":"Call ahead 30 minutes"},{"notes":""},{"notes":"Liftgate required"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenArrayNameCannotBeDetermined()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "no-wildcard-path",
            TargetPath = "result",
            TransformationConfig = null
        };

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenArrayIsEmpty()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "stops[*].notes", TargetPath = "notes" };

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(JsonDocument.Parse("[]").RootElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ExtractsWholeItems_WhenNoItemField()
    {
        // fm-mcleod-110: SourceArrayPath only, no ItemField – returns full stop objects
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "notes",
            TransformationConfig = """{"SourceArrayPath":"stops"}"""
        };

        var json = """[{"notes":"Note A"},{"notes":"Note B"},{"notes":"Note C"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
    }
}
