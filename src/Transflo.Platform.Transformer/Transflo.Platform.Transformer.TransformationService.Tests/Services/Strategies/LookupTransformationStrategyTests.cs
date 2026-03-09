using Microsoft.Extensions.Logging;
using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class LookupTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly Mock<ILookupDataProvider> _lookupProviderMock = new();
    private readonly Mock<ILogger<LookupTransformationStrategy>> _loggerMock = new();
    private readonly LookupTransformationStrategy _sut;

    public LookupTransformationStrategyTests()
    {
        _sut = new LookupTransformationStrategy(
            _jsonParserMock.Object,
            _lookupProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void TransformationType_Returns_Lookup()
    {
        Assert.Equal(TransformationType.Lookup, _sut.TransformationType);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsMappedValue_WhenKeyFound()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "statusCode",
            TransformationConfig = """{"LookupTableId":"tbl-1"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("ACTIVE");
        _lookupProviderMock
            .Setup(p => p.GetAsync("tbl-1"))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"ACTIVE":"A","INACTIVE":"I"}""",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("A", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsLookupDefault_WhenKeyNotFound()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "statusCode",
            TransformationConfig = """{"LookupTableId":"tbl-1"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("UNKNOWN");
        _lookupProviderMock
            .Setup(p => p.GetAsync("tbl-1"))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"ACTIVE":"A"}""",
                DefaultValue = "X",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("X", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsSourceValue_WhenKeyNotFoundAndNoDefault()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "statusCode",
            TransformationConfig = """{"LookupTableId":"tbl-1"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("UNKNOWN");
        _lookupProviderMock
            .Setup(p => p.GetAsync("tbl-1"))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"ACTIVE":"A"}""",
                DefaultValue = null,
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("UNKNOWN", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenSourceValueIsNull()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "statusCode",
            TransformationConfig = """{"LookupTableId":"tbl-1"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync((object?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
        _lookupProviderMock.Verify(p => p.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsSourceValue_WhenNoLookupTableIdInConfig()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "statusCode",
            TransformationConfig = """{"SomeOtherKey":"value"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("ACTIVE");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("ACTIVE", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsSourceValue_WhenLookupDataNotFound()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "statusCode",
            TransformationConfig = """{"LookupTableId":"missing-tbl"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("ACTIVE");
        _lookupProviderMock
            .Setup(p => p.GetAsync("missing-tbl"))
            .ReturnsAsync((LookupData?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("ACTIVE", result);
    }

    [Fact]
    public async Task ApplyAsync_IsCaseSensitive_WhenConfiguredAsSuch()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "statusCode",
            TransformationConfig = """{"LookupTableId":"tbl-cs"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("active");
        _lookupProviderMock
            .Setup(p => p.GetAsync("tbl-cs"))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"ACTIVE":"A"}""",
                DefaultValue = "DEFAULT",
                IsCaseSensitive = true
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("DEFAULT", result);
    }

    [Fact]
    public async Task ApplyAsync_IsCaseInsensitive_WhenConfiguredAsSuch()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "statusCode",
            TransformationConfig = """{"LookupTableId":"tbl-ci"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("active");
        _lookupProviderMock
            .Setup(p => p.GetAsync("tbl-ci"))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"ACTIVE":"A"}""",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("A", result);
    }
}
