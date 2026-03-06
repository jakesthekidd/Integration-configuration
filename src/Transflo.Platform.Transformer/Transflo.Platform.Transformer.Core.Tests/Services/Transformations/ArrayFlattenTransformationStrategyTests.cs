using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.TransformationStrategies;
using Xunit;

namespace Transflo.Platform.Transformer.Core.Tests.Services.Transformations;

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
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "allEmails",
            TransformationConfig = """{"SourceArrayPath":"contacts","ItemField":"email"}"""
        };

        var json = """[{"email":"a@test.com"},{"email":"b@test.com"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "contacts"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal("a@test.com", list[0]);
        Assert.Equal("b@test.com", list[1]);
    }

    [Fact]
    public async Task ApplyAsync_FlattensArrayField_UsingWildcardSourcePath()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "stops[*].city",
            TargetPath = "cities"
        };

        var json = """[{"city":"Atlanta"},{"city":"Dallas"},{"city":"Miami"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "stops"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal("Atlanta", list[0]);
        Assert.Equal("Dallas", list[1]);
        Assert.Equal("Miami", list[2]);
    }

    [Fact]
    public async Task ApplyAsync_FiltersEmptyValues_WhenFilterEmptyIsTrue()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "items[*].code",
            TargetPath = "codes",
            TransformationConfig = """{"FilterEmpty":"true"}"""
        };

        var json = """[{"code":"A"},{"code":""},{"code":"C"}]""";
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

    [Fact]
    public async Task ApplyAsync_IncludesEmptyValues_WhenFilterEmptyIsFalse()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "items[*].code",
            TargetPath = "codes",
            TransformationConfig = """{"FilterEmpty":"false"}"""
        };

        var json = """[{"code":"A"},{"code":""},{"code":"C"}]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "items"))
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
        var mapping = new FieldMapping { SourcePath = "items[*].id", TargetPath = "ids" };

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "items"))
            .ReturnsAsync(JsonDocument.Parse("[]").RootElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ExtractsWholeItems_WhenNoItemField()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "values",
            TransformationConfig = """{"SourceArrayPath":"numbers"}"""
        };

        var json = """[1,2,3]""";
        var arrayElement = JsonDocument.Parse(json).RootElement;

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "numbers"))
            .ReturnsAsync(arrayElement);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        var list = Assert.IsType<List<object>>(result);
        Assert.Equal(3, list.Count);
    }
}
