using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

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
        // fm-mcleod-044: driver full name from first + last name with space separator
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "carrier.driverName",
            TransformationConfig = """{"Fields":["movement[0].driver_first_name","movement[0].driver_last_name"]}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "movement[0].driver_first_name")).ReturnsAsync("John");
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "movement[0].driver_last_name")).ReturnsAsync("Doe");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("John Doe", result);
    }

    [Fact]
    public async Task ApplyAsync_ConcatenatesFields_WithCustomSeparator()
    {
        // fm-mcleod-034: customer address from address1 + address2 with ", " separator
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "customer.address",
            TransformationConfig = """{"Fields":["customer.address1","customer.address2"],"Separator":", "}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "customer.address1")).ReturnsAsync("100 Commerce Blvd");
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "customer.address2")).ReturnsAsync("Suite 200");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("100 Commerce Blvd, Suite 200", result);
    }

    [Fact]
    public async Task ApplyAsync_SkipsEmptyValues_WhenSkipEmptyIsTrue()
    {
        // fm-mcleod-034: address2 is empty/null and should be omitted from the result
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "customer.address",
            TransformationConfig = """{"Fields":["customer.address1","customer.address2","customer.city"],"Separator":", ","SkipEmpty":"true"}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "customer.address1")).ReturnsAsync("100 Commerce Blvd");
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "customer.address2")).ReturnsAsync((object?)null);
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "customer.city")).ReturnsAsync("Atlanta");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("100 Commerce Blvd, Atlanta", result);
    }

    [Fact]
    public async Task ApplyAsync_IncludesEmptyValues_WhenSkipEmptyIsFalse()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "customer.address",
            TransformationConfig = """{"Fields":["customer.address1","customer.address2","customer.city"],"Separator":", ","SkipEmpty":"false"}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "customer.address1")).ReturnsAsync("100 Commerce Blvd");
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "customer.address2")).ReturnsAsync((object?)null);
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "customer.city")).ReturnsAsync("Atlanta");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("100 Commerce Blvd, Atlanta", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConfigIsNull()
    {
        var mapping = new FieldMapping { SourcePath = "", TargetPath = "carrier.driverName", TransformationConfig = null };
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
            TargetPath = "carrier.driverName",
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
            TargetPath = "carrier.driverName",
            TransformationConfig = """{"Fields":["movement[0].driver_first_name","movement[0].driver_last_name"]}"""
        };
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "movement[0].driver_first_name")).ReturnsAsync((object?)null);
        _jsonParserMock.Setup(p => p.GetValueAtPathAsync(sourceData, "movement[0].driver_last_name")).ReturnsAsync((object?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal(string.Empty, result);
    }
}
