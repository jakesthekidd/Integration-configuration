using Microsoft.Extensions.Logging;
using Moq;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

namespace Transflo.Platform.Transformer.Core.Tests.Services.Transformations;

public class LookupTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly Mock<ILookupTableRepository> _lookupRepoMock = new();
    private readonly Mock<ILogger<LookupTransformationStrategy>> _loggerMock = new();
    private readonly LookupTransformationStrategy _sut;

    public LookupTransformationStrategyTests()
    {
        _sut = new LookupTransformationStrategy(
            _jsonParserMock.Object,
            _lookupRepoMock.Object,
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
        _lookupRepoMock
            .Setup(r => r.GetByIdAsync("tbl-1"))
            .ReturnsAsync(new LookupTable
            {
                Id = "tbl-1",
                TmsSystemId = "sys-1",
                FieldName = "status",
                Name = "Status Lookup",
                Mappings = """{"ACTIVE":"A","INACTIVE":"I"}""",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("A", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsLookupTableDefault_WhenKeyNotFound()
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
        _lookupRepoMock
            .Setup(r => r.GetByIdAsync("tbl-1"))
            .ReturnsAsync(new LookupTable
            {
                Id = "tbl-1",
                TmsSystemId = "sys-1",
                FieldName = "status",
                Name = "Status Lookup",
                Mappings = """{"ACTIVE":"A"}""",
                DefaultValue = "X",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("X", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsSourceValue_WhenKeyNotFoundAndNoTableDefault()
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
        _lookupRepoMock
            .Setup(r => r.GetByIdAsync("tbl-1"))
            .ReturnsAsync(new LookupTable
            {
                Id = "tbl-1",
                TmsSystemId = "sys-1",
                FieldName = "status",
                Name = "Status Lookup",
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
        _lookupRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
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
    public async Task ApplyAsync_ReturnsSourceValue_WhenLookupTableNotFound()
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
        _lookupRepoMock
            .Setup(r => r.GetByIdAsync("missing-tbl"))
            .ReturnsAsync((LookupTable?)null);

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        Assert.Equal("ACTIVE", result);
    }

    [Fact]
    public async Task ApplyAsync_IsCaseSensitive_WhenLookupTableConfiguredAsSuch()
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
            .ReturnsAsync("active"); // lowercase, table has "ACTIVE"
        _lookupRepoMock
            .Setup(r => r.GetByIdAsync("tbl-cs"))
            .ReturnsAsync(new LookupTable
            {
                Id = "tbl-cs",
                TmsSystemId = "sys-1",
                FieldName = "status",
                Name = "Status Lookup",
                Mappings = """{"ACTIVE":"A"}""",
                DefaultValue = "DEFAULT",
                IsCaseSensitive = true
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        // Case-sensitive: "active" != "ACTIVE", so returns default
        Assert.Equal("DEFAULT", result);
    }

    [Fact]
    public async Task ApplyAsync_IsCaseInsensitive_WhenLookupTableConfiguredAsSuch()
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
            .ReturnsAsync("active"); // lowercase, table has "ACTIVE"
        _lookupRepoMock
            .Setup(r => r.GetByIdAsync("tbl-ci"))
            .ReturnsAsync(new LookupTable
            {
                Id = "tbl-ci",
                TmsSystemId = "sys-1",
                FieldName = "status",
                Name = "Status Lookup",
                Mappings = """{"ACTIVE":"A"}""",
                IsCaseSensitive = false
            });

        var result = await _sut.ApplyAsync(new TransformationContext { SourceData = sourceData, Mapping = mapping });

        // Case-insensitive: "active" matches "ACTIVE"
        Assert.Equal("A", result);
    }
}
