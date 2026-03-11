using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using ServiceModels = Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class TransformationCoordinatorTests
{
    private readonly Mock<ITemplateRepository> _templateRepoMock = new();
    private readonly Mock<IFieldMappingRepository> _mappingRepoMock = new();
    private readonly Mock<ITransformationLogRepository> _logRepoMock = new();
    private readonly Mock<ITransformationService> _serviceMock = new();
    private readonly Mock<ILogger<TransformationCoordinator>> _loggerMock = new();
    private readonly TransformationCoordinator _sut;

    // Core EF entities used by repository mocks – mirror the McLeod seed template
    private static readonly FieldMappingTemplate DefaultEfTemplate = new()
    {
        TemplateId = new Guid("00000000-0000-0000-0000-000000000001"),
        TmsSystemId = new Guid("00000000-0000-0000-0000-000000000002"),
        Name = "McLeod to WFAI Transformation",
        Version = 1
    };

    private static readonly List<FieldMapping> DefaultEfMappings =
    [
        // fm-mcleod-001: Direct copy of order ID
        new() { SourcePath = "id", TargetPath = "externalId" }
    ];

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
        var templateId = new Guid("00000000-0000-0000-0000-000000000003");
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(templateId))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.TransformAsync("{}", templateId);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSpecificVersionNotFound()
    {
        var templateId = new Guid("00000000-0000-0000-0000-000000000001");
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId, 99))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.TransformAsync("{}", templateId, version: 99);

        Assert.False(result.Success);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenNoMappingsFound()
    {
        var templateId = new Guid("00000000-0000-0000-0000-000000000001");
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(templateId))
            .ReturnsAsync(DefaultEfTemplate);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(DefaultEfTemplate.Id))
            .ReturnsAsync(new List<FieldMapping>());

        var result = await _sut.TransformAsync("{}", templateId);

        Assert.False(result.Success);
        Assert.Equal("NO_MAPPINGS", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_DelegatesTo_TransformationService()
    {
        var templateId = new Guid("00000000-0000-0000-0000-000000000001");
        SetupResolve(templateId);
        var serviceResult = new TransformationResult { Success = true, FieldsMapped = 1 };
        _serviceMock
            .Setup(s => s.TransformAsync(
                """{"id":"3089050","status":"D"}""",
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(serviceResult);

        var result = await _sut.TransformAsync("""{"id":"3089050","status":"D"}""", templateId);

        Assert.True(result.Success);
        Assert.Equal(1, result.FieldsMapped);
    }

    [Fact]
    public async Task TransformAsync_PersistsLog()
    {
        var templateId = new Guid("00000000-0000-0000-0000-000000000001");
        SetupResolve(templateId);
        _serviceMock
            .Setup(s => s.TransformAsync(
                It.IsAny<string>(),
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(new TransformationResult { Success = true });

        await _sut.TransformAsync("""{"id":"3089050","status":"D"}""", templateId);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
    }

    [Fact]
    public async Task PreviewTransformationAsync_DoesNotPersistLog()
    {
        var templateId = new Guid("00000000-0000-0000-0000-000000000001");
        SetupResolve(templateId);
        _serviceMock
            .Setup(s => s.TransformAsync(
                It.IsAny<string>(),
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(new TransformationResult { Success = true });

        await _sut.PreviewTransformationAsync("""{"id":"3089050","status":"D"}""", templateId);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Never);
    }

    [Fact]
    public async Task TransformBatchAsync_ReturnsError_WhenTemplateNotFound()
    {
        var templateId = new Guid("00000000-0000-0000-0000-000000000003");
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(templateId))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse("""{"id":"3089050"}""").RootElement,
            JsonDocument.Parse("""{"id":"3089051"}""").RootElement
        };

        var result = await _sut.TransformBatchAsync(templateId, records);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.ErrorCount);
        Assert.All(result.Results, r => Assert.False(r.Success));
    }

    [Fact]
    public async Task TransformBatchAsync_PersistsSingleSummaryLog()
    {
        var templateId = new Guid("00000000-0000-0000-0000-000000000001");
        SetupResolve(templateId);
        var batchResult = new BatchTransformResult
        {
            TemplateId = templateId,
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
            JsonDocument.Parse("""{"id":"3089050"}""").RootElement,
            JsonDocument.Parse("""{"id":"3089051"}""").RootElement
        };

        await _sut.TransformBatchAsync(new Guid("00000000-0000-0000-0000-000000000001"), records);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
    }

    private void SetupResolve(Guid templateId)
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(templateId))
            .ReturnsAsync(DefaultEfTemplate);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(DefaultEfTemplate.Id))
            .ReturnsAsync(DefaultEfMappings);
    }
}
