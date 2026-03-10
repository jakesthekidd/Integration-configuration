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
        // fm-mcleod-021: movement[*].movement_id → movementNumbers array
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "movement[*].movement_id",
            TargetPath = "movementNumbers"
        };

        var json = """[{"movement_id":"MOV-9050-1"},{"movement_id":"MOV-9050-2"},{"movement_id":"MOV-9050-3"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "movement"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal("MOV-9050-1", list[0]);
        Assert.Equal("MOV-9050-2", list[1]);
        Assert.Equal("MOV-9050-3", list[2]);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsWholeElements_WhenNoItemField()
    {
        // stops[*] without a sub-field returns full stop objects
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "stops[*]",
            TargetPath = "allStops"
        };

        var json = """[{"stop_type":"PU","city":"Memphis"},{"stop_type":"SO","city":"Dallas"},{"stop_type":"SO","city":"Atlanta"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenSourcePathHasNoWildcard()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "movement.movement_id", TargetPath = "movementNumbers" };

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenArrayIsEmpty()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "movement[*].movement_id", TargetPath = "movementNumbers" };

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "movement"))
            .ReturnsAsync(JsonDocument.Parse("[]").RootElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenArrayValueIsNull()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "movement[*].movement_id", TargetPath = "movementNumbers" };

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "movement"))
            .ReturnsAsync((object?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_SkipsNullItems_InArray()
    {
        // stops[*].company_name extracts names, skipping null stop entries
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "stops[*].company_name", TargetPath = "locationNames" };

        var json = """[{"company_name":"Origin Warehouse"},null,{"company_name":"Destination DC"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal("Origin Warehouse", list[0]);
        Assert.Equal("Destination DC", list[1]);
    }
}
