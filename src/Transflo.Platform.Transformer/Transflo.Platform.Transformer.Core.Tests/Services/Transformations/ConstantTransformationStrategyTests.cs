using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

namespace Transflo.Platform.Transformer.Core.Tests.Services.Transformations;

public class ConstantTransformationStrategyTests
{
    private readonly ConstantTransformationStrategy _sut = new();

    [Fact]
    public void TransformationType_Returns_Constant()
    {
        Assert.Equal(TransformationType.Constant, _sut.TransformationType);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsDefaultValue()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "status",
            DefaultValue = "ACTIVE"
        };
        var context = new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = mapping
        };

        var result = await _sut.ApplyAsync(context);

        Assert.Equal("ACTIVE", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenDefaultValueIsNull()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "status",
            DefaultValue = null
        };
        var context = new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = mapping
        };

        var result = await _sut.ApplyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_DoesNotReadSourceData()
    {
        // Constant strategy must not interact with source data
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "any.path", TargetPath = "target", DefaultValue = "fixed" };
        var context = new TransformationContext { SourceData = sourceData, Mapping = mapping };

        var result = await _sut.ApplyAsync(context);

        Assert.Equal("fixed", result);
        Assert.Empty(sourceData); // Not modified
    }
}
