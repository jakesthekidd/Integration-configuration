using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using ServiceModels = Transflo.Platform.Transformer.TransformationService.Models;
using Xunit;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class TransformationCoordinatorTests
{
    private readonly Mock<ITemplateRepository> _templateRepoMock = new();
    private readonly Mock<IFieldMappingRepository> _mappingRepoMock = new();
    private readonly Mock<ITransformationLogRepository> _logRepoMock = new();
    private readonly Mock<ITransformationService> _serviceMock = new();
    private readonly Mock<ILogger<TransformationCoordinator>> _loggerMock = new();
    private readonly TransformationCoordinator _sut;

    // Core EF entities used by repository mocks
    private static readonly FieldMappingTemplate DefaultEfTemplate = new()
    {
        TemplateId = "tmpl-1",
        TmsSystemId = "sys-1",
        Name = "Test Template",
        Version = 1
    };

    private static readonly List<FieldMapping> DefaultEfMappings = new()
    {
        new() { SourcePath = "name", TargetPath = "fullName" }
    };

    public TransformationCoordinatorTests()
    {
        _sut = new TransformationCoordinator(
            _templateRepoMock.Object,
            _mappingRepoMock.Object,
            _logRepoMock.Object,
            _serviceMock.Object,
            _loggerMock.Object);

        _logRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TransformationLog>()))
            .ReturnsAsync((TransformationLog log) => log);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenTemplateNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync("tmpl-missing"))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.TransformAsync("{}", "tmpl-missing");

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSpecificVersionNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync("tmpl-1", 99))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.TransformAsync("{}", "tmpl-1", version: 99);

        Assert.False(result.Success);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenNoMappingsFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync("tmpl-1"))
            .ReturnsAsync(DefaultEfTemplate);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateIdOrderedAsync("tmpl-1"))
            .ReturnsAsync(new List<FieldMapping>());

        var result = await _sut.TransformAsync("{}", "tmpl-1");

        Assert.False(result.Success);
        Assert.Equal("NO_MAPPINGS", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_DelegatesTo_TransformationService()
    {
        SetupResolve();
        var serviceResult = new TransformationResult { Success = true, FieldsMapped = 1 };
        _serviceMock
            .Setup(s => s.TransformAsync(
                "{}",
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(serviceResult);

        var result = await _sut.TransformAsync("{}", "tmpl-1");

        Assert.True(result.Success);
        Assert.Equal(1, result.FieldsMapped);
    }

    [Fact]
    public async Task TransformAsync_PersistsLog()
    {
        SetupResolve();
        _serviceMock
            .Setup(s => s.TransformAsync(
                It.IsAny<string>(),
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(new TransformationResult { Success = true });

        await _sut.TransformAsync("{}", "tmpl-1");

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
    }

    [Fact]
    public async Task PreviewTransformationAsync_DoesNotPersistLog()
    {
        SetupResolve();
        _serviceMock
            .Setup(s => s.TransformAsync(
                It.IsAny<string>(),
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(new TransformationResult { Success = true });

        await _sut.PreviewTransformationAsync("{}", "tmpl-1");

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Never);
    }

    [Fact]
    public async Task TransformBatchAsync_ReturnsError_WhenTemplateNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync("tmpl-missing"))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse("{}").RootElement,
            JsonDocument.Parse("{}").RootElement
        };

        var result = await _sut.TransformBatchAsync("tmpl-missing", records);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.ErrorCount);
        Assert.All(result.Results, r => Assert.False(r.Success));
    }

    [Fact]
    public async Task TransformBatchAsync_PersistsSingleSummaryLog()
    {
        SetupResolve();
        var batchResult = new BatchTransformResult
        {
            TemplateId = "tmpl-1",
            TotalRecords = 2,
            SuccessCount = 2,
            Results = new List<BatchRecordResult>
            {
                new() { Index = 0, Success = true },
                new() { Index = 1, Success = true }
            }
        };
        _serviceMock
            .Setup(s => s.TransformBatchAsync(
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>(),
                It.IsAny<List<JsonElement>>()))
            .ReturnsAsync(batchResult);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse("{}").RootElement,
            JsonDocument.Parse("{}").RootElement
        };

        await _sut.TransformBatchAsync("tmpl-1", records);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
    }

    private void SetupResolve()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync("tmpl-1"))
            .ReturnsAsync(DefaultEfTemplate);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateIdOrderedAsync("tmpl-1"))
            .ReturnsAsync(DefaultEfMappings);
    }
}
