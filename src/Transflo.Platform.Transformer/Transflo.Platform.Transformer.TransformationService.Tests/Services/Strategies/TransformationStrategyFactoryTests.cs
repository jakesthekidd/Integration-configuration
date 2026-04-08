using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class TransformationStrategyFactoryTests
{
    private static Mock<ITransformationStrategy> CreateStrategyMock(TransformationType type)
    {
        var mock = new Mock<ITransformationStrategy>();
        mock.Setup(s => s.TransformationType).Returns(type);
        return mock;
    }

    [Fact]
    public void GetStrategy_ReturnsCorrectStrategy_ForRegisteredType()
    {
        var directMock = CreateStrategyMock(TransformationType.Direct);
        var concatMock = CreateStrategyMock(TransformationType.Concat);
        var factory = new TransformationStrategyFactory(
            new ITransformationStrategy[] { directMock.Object, concatMock.Object });

        var result = factory.GetStrategy(TransformationType.Direct);

        Assert.Same(directMock.Object, result);
    }

    [Fact]
    public void GetStrategy_ReturnsNull_ForUnregisteredType()
    {
        var directMock = CreateStrategyMock(TransformationType.Direct);
        var factory = new TransformationStrategyFactory(
            new ITransformationStrategy[] { directMock.Object });

        var result = factory.GetStrategy(TransformationType.Math);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(TransformationType.Direct)]
    [InlineData(TransformationType.Constant)]
    [InlineData(TransformationType.Lookup)]
    [InlineData(TransformationType.Concat)]
    [InlineData(TransformationType.DateFormat)]
    [InlineData(TransformationType.ArrayMap)]
    [InlineData(TransformationType.ArrayFlatten)]
    [InlineData(TransformationType.Conditional)]
    [InlineData(TransformationType.Substring)]
    [InlineData(TransformationType.Template)]
    [InlineData(TransformationType.Math)]
    [InlineData(TransformationType.PrefixMap)]
    [InlineData(TransformationType.ConditionalDateFormat)]
    public void GetStrategy_ReturnsStrategy_ForEachRegisteredType(TransformationType type)
    {
        var strategies = new[]
        {
            TransformationType.Direct,
            TransformationType.Constant,
            TransformationType.Lookup,
            TransformationType.Concat,
            TransformationType.DateFormat,
            TransformationType.ArrayMap,
            TransformationType.ArrayFlatten,
            TransformationType.Conditional,
            TransformationType.Substring,
            TransformationType.Template,
            TransformationType.Math,
            TransformationType.PrefixMap,
            TransformationType.ConditionalDateFormat
        }.Select(CreateStrategyMock).Select(m => m.Object);

        var factory = new TransformationStrategyFactory(strategies);

        var result = factory.GetStrategy(type);

        Assert.NotNull(result);
        Assert.Equal(type, result.TransformationType);
    }

    [Theory]
    [InlineData((TransformationType)999)]
    public void GetStrategy_ReturnsNull_ForUnknownType(TransformationType type)
    {
        var directMock = CreateStrategyMock(TransformationType.Direct);
        var factory = new TransformationStrategyFactory(
            new ITransformationStrategy[] { directMock.Object });

        var result = factory.GetStrategy(type);

        Assert.Null(result);
    }

    [Fact]
    public void Constructor_HandlesEmptyStrategiesList()
    {
        var factory = new TransformationStrategyFactory(Enumerable.Empty<ITransformationStrategy>());

        var result = factory.GetStrategy(TransformationType.Direct);

        Assert.Null(result);
    }
}
