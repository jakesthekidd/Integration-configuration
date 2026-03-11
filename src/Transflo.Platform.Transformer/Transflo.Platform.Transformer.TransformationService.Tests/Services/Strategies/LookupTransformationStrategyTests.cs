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
        // fm-mcleod-002: status "D" → "Delivered" via lut-mcleod-order-status
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "status",
            TransformationConfig = """{"LookupTableId":"00000000-0000-0000-0000-000000000001"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("D");
        _lookupProviderMock
            .Setup(p => p.GetAsync(new Guid("00000000-0000-0000-0000-000000000001")))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"D":"Delivered","A":"Available","P":"In Progress","C":"Cancelled"}""",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("Delivered", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsLookupDefault_WhenKeyNotFound()
    {
        // fm-mcleod-002: unknown status code falls back to lookup default value
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "status",
            TransformationConfig = """{"LookupTableId":"00000000-0000-0000-0000-000000000001"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("Z");
        _lookupProviderMock
            .Setup(p => p.GetAsync(new Guid("00000000-0000-0000-0000-000000000001")))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"D":"Delivered","A":"Available"}""",
                DefaultValue = "Unknown",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("Unknown", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsSourceValue_WhenKeyNotFoundAndNoDefault()
    {
        // fm-mcleod-002: unknown status code returned as-is when no default is configured
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "status",
            TransformationConfig = """{"LookupTableId":"00000000-0000-0000-0000-000000000001"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("Z");
        _lookupProviderMock
            .Setup(p => p.GetAsync(new Guid("00000000-0000-0000-0000-000000000001")))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"D":"Delivered","A":"Available"}""",
                DefaultValue = null,
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("Z", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenSourceValueIsNull()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "status",
            TransformationConfig = """{"LookupTableId":"00000000-0000-0000-0000-000000000001"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync((object?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Null(result);
        _lookupProviderMock.Verify(p => p.GetAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsSourceValue_WhenNoLookupTableIdInConfig()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "status",
            TransformationConfig = """{"SomeOtherKey":"value"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("D");

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("D", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsSourceValue_WhenLookupDataNotFound()
    {
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "status",
            TransformationConfig = """{"LookupTableId":"00000000-0000-0000-0000-000000000002"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("D");
        _lookupProviderMock
            .Setup(p => p.GetAsync(new Guid("00000000-0000-0000-0000-000000000002")))
            .ReturnsAsync((LookupData?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("D", result);
    }

    [Fact]
    public async Task ApplyAsync_IsCaseSensitive_WhenConfiguredAsSuch()
    {
        // fm-mcleod-002: lowercase "d" should NOT match "D" when case-sensitive
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "status",
            TransformationConfig = """{"LookupTableId":"00000000-0000-0000-0000-000000000001"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("d");
        _lookupProviderMock
            .Setup(p => p.GetAsync(new Guid("00000000-0000-0000-0000-000000000001")))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"D":"Delivered","A":"Available"}""",
                DefaultValue = "Unknown",
                IsCaseSensitive = true
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("Unknown", result);
    }

    [Fact]
    public async Task ApplyAsync_IsCaseInsensitive_WhenConfiguredAsSuch()
    {
        // fm-mcleod-002: lowercase "d" matches "D" when case-insensitive
        var sourceData = new Dictionary<string, object>();
        var mapping = new FieldMapping
        {
            SourcePath = "status",
            TargetPath = "status",
            TransformationConfig = """{"LookupTableId":"00000000-0000-0000-0000-000000000001"}"""
        };
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(sourceData, "status"))
            .ReturnsAsync("d");
        _lookupProviderMock
            .Setup(p => p.GetAsync(new Guid("00000000-0000-0000-0000-000000000001")))
            .ReturnsAsync(new LookupData
            {
                Mappings = """{"D":"Delivered","A":"Available"}""",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("Delivered", result);
    }
}
