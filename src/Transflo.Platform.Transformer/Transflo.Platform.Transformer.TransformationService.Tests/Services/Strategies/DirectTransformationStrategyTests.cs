using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

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
        // fm-mcleod-001: id → externalId (Direct copy of McLeod order ID)
        var sourceData = new Dictionary<string, object> { ["id"] = "3089050" };
        var mapping = new FieldMapping { SourcePath = "id", TargetPath = "externalId" };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "id"))
            .ReturnsAsync("3089050");

        var context = new TransformationContext { SourceData = sourceData, Mapping = mapping };
        var result = await _sut.ApplyAsync(context);

        Assert.Equal("3089050", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenPathNotFound()
    {
        // fm-mcleod-022: pro_number is optional and may be absent from the payload
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "pro_number", TargetPath = "proNumber" };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "pro_number"))
            .ReturnsAsync((object?)null);

        var context = new TransformationContext { SourceData = sourceData, Mapping = mapping };
        var result = await _sut.ApplyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_PassesCorrectSourcePathToParser()
    {
        // fm-mcleod-033: customer.email is a nested path
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "customer.email", TargetPath = "customer.email" };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "customer.email"))
            .ReturnsAsync("billing@wfai.com");

        var context = new TransformationContext { SourceData = sourceData, Mapping = mapping };
        await _sut.ApplyAsync(context);

        _jsonParserMock.Verify(p => p.GetValueAtPathAsync(sourceData, "customer.email"), Times.Once);
    }
}
