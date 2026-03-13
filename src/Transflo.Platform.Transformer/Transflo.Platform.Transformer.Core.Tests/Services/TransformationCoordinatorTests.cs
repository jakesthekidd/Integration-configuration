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
    private readonly Mock<ITemplateVersionRepository> _versionRepoMock = new();
    private readonly Mock<ILogger<TransformationCoordinator>> _loggerMock = new();
    private readonly TransformationCoordinator _sut;

    private static readonly Template DefaultEfTemplate = new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000001"),
        Name = "McLeod to WFAI Transformation"
    };

    private static readonly TemplateVersion DefaultEfVersion = new()
    {
        Id = new Guid("00000000-0000-0000-0000-00000000000A"),
        TemplateId = DefaultEfTemplate.Id,
        Version = 1,
        Status = TemplateVersionStatus.Published
    };

    private static readonly List<FieldMapping> DefaultEfMappings =
    [
        new() { SourcePath = "id", TargetPath = "externalId", TemplateVersionId = DefaultEfVersion.Id }
    ];

    public TransformationCoordinatorTests()
    {
        _sut = new TransformationCoordinator(
            _templateRepoMock.Object,
            _mappingRepoMock.Object,
            _logRepoMock.Object,
            _serviceMock.Object,
            _versionRepoMock.Object,
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
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync((Template?)null);

        var result = await _sut.TransformAsync("{}", templateId);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSpecificVersionNotFound()
    {
        var templateId = DefaultEfTemplate.Id;
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync(DefaultEfTemplate);
        _versionRepoMock
            .Setup(r => r.GetByVersionAsync(templateId, 99))
            .ReturnsAsync((TemplateVersion?)null);

        var result = await _sut.TransformAsync("{}", templateId, version: 99);

        Assert.False(result.Success);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenNoMappingsFound()
    {
        var templateId = DefaultEfTemplate.Id;
        SetupResolve(templateId);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(DefaultEfVersion.Id))
            .ReturnsAsync(new List<FieldMapping>());

        var result = await _sut.TransformAsync("{}", templateId);

        Assert.False(result.Success);
        Assert.Equal("NO_MAPPINGS", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_DelegatesTo_TransformationService()
    {
        var templateId = DefaultEfTemplate.Id;
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
        var templateId = DefaultEfTemplate.Id;
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
        var templateId = DefaultEfTemplate.Id;
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
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync((Template?)null);

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
        var templateId = DefaultEfTemplate.Id;
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

        await _sut.TransformBatchAsync(templateId, records);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
    }

    private void SetupResolve(Guid templateId)
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync(DefaultEfTemplate);
        _versionRepoMock
            .Setup(r => r.GetPublishedVersionAsync(templateId))
            .ReturnsAsync(DefaultEfVersion);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(DefaultEfVersion.Id))
            .ReturnsAsync(DefaultEfMappings);
    }
}
