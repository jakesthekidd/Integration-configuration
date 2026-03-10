using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class ArrayMapTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly Mock<ILogger<ArrayMapTransformationStrategy>> _loggerMock = new();
    private readonly ArrayMapTransformationStrategy _sut;

    public ArrayMapTransformationStrategyTests()
    {
        _sut = new ArrayMapTransformationStrategy(_jsonParserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void TransformationType_Returns_ArrayMap()
    {
        Assert.Equal(TransformationType.ArrayMap, _sut.TransformationType);
    }

    [Fact]
    public async Task ApplyAsync_ExtractsFieldFromEachArrayElement()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "movements[*].movement_id",
            TargetPath = "ids"
        };

        var json = """[{"movement_id":"M1"},{"movement_id":"M2"},{"movement_id":"M3"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "movements"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal("M1", list[0]);
        Assert.Equal("M2", list[1]);
        Assert.Equal("M3", list[2]);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsWholeElements_WhenNoItemField()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "tags[*]",
            TargetPath = "allTags"
        };

        var json = """["alpha","beta","gamma"]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "tags"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenSourcePathHasNoWildcard()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "movements.movement_id", TargetPath = "ids" };

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenArrayIsEmpty()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "items[*].id", TargetPath = "ids" };

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "items"))
            .ReturnsAsync(JsonDocument.Parse("[]").RootElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenArrayValueIsNull()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "items[*].id", TargetPath = "ids" };

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "items"))
            .ReturnsAsync((object?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_SkipsNullItems_InArray()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "items[*].id", TargetPath = "ids" };

        var json = """[{"id":"A"},null,{"id":"C"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "items"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal("A", list[0]);
        Assert.Equal("C", list[1]);
    }
}
