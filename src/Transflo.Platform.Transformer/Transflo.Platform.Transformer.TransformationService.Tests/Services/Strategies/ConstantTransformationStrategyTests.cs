using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

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
        // fm-mcleod-038: customer.country is always "US" for domestic McLeod orders
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "customer.country",
            DefaultValue = "US"
        };
        var context = new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = mapping
        };

        var result = await _sut.ApplyAsync(context);

        Assert.Equal("US", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenDefaultValueIsNull()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "customer.country",
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
        // fm-mcleod-047: carrier.country is always "US" regardless of source data
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping { SourcePath = "any.path", TargetPath = "carrier.country", DefaultValue = "US" };
        var context = new TransformationContext { SourceData = sourceData, Mapping = mapping };

        var result = await _sut.ApplyAsync(context);

        Assert.Equal("US", result);
        Assert.Empty(sourceData);
    }
}
