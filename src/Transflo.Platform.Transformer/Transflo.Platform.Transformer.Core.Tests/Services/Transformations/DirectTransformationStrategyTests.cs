using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.TransformationStrategies;
using Xunit;

namespace Transflo.Platform.Transformer.Core.Tests.Services.Transformations;

public class DirectTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly DirectTransformationStrategy _sut;

    public DirectTransformationStrategyTests()
    {
        _sut = new DirectTransformationStrategy(_jsonParserMock.Object);
    }

    [Fact]
    public void TransformationType_Returns_Direct()
    {
        Assert.Equal(TransformationType.Direct, _sut.TransformationType);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsValueFromJsonParser()
    {
        var sourceData = new Dictionary<string, object> { ["name"] = "John" };
        var mapping = new FieldMapping { SourcePath = "name", TargetPath = "fullName" };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "name"))
            .ReturnsAsync("John");

        var context = new TransformationContext { SourceData = sourceData, Mapping = mapping };
        var result = await _sut.ApplyAsync(context);

        Assert.Equal("John", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenPathNotFound()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "missing.path", TargetPath = "target" };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "missing.path"))
            .ReturnsAsync((object?)null);

        var context = new TransformationContext { SourceData = sourceData, Mapping = mapping };
        var result = await _sut.ApplyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_PassesCorrectSourcePathToParser()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "order.customer.email", TargetPath = "email" };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "order.customer.email"))
            .ReturnsAsync("test@example.com");

        var context = new TransformationContext { SourceData = sourceData, Mapping = mapping };
        await _sut.ApplyAsync(context);

        _jsonParserMock.Verify(p => p.GetValueAtPathAsync(sourceData, "order.customer.email"), Times.Once);
    }
}
