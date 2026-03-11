using Moq;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class TemplatesServiceTests
{
    private readonly Mock<ITemplateRepository> _templateRepoMock = new();
    private readonly Mock<IFieldMappingRepository> _mappingRepoMock = new();
    private readonly TemplatesService _sut;

    private static readonly FieldMappingTemplate SampleTemplate = new()
    {
        Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
        TemplateId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
        Name = "Test Template",
        Description = "Desc",
        TmsSystemId = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
        Version = 1,
        Status = TemplateStatus.Draft
    };

    public TemplatesServiceTests()
    {
        _sut = new TemplatesService(_templateRepoMock.Object, _mappingRepoMock.Object);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoFilter_CallsGetAllAsync()
    {
        _templateRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([SampleTemplate]);

        var result = await _sut.GetAllAsync();

        _templateRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        Assert.Single(result);
        Assert.Equal(SampleTemplate.TemplateId, result[0].TemplateId);
    }

    [Fact]
    public async Task GetAllAsync_WithTmsSystemId_CallsGetByTmsSystemIdAsync()
    {
        var tmsId = SampleTemplate.TmsSystemId;
        _templateRepoMock.Setup(r => r.GetByTmsSystemIdAsync(tmsId)).ReturnsAsync([SampleTemplate]);

        var result = await _sut.GetAllAsync(tmsId);

        _templateRepoMock.Verify(r => r.GetByTmsSystemIdAsync(tmsId), Times.Once);
        Assert.Single(result);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), null))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsResponse_WhenFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.TemplateId, null))
            .ReturnsAsync(SampleTemplate);

        var result = await _sut.GetByIdAsync(SampleTemplate.TemplateId);

        Assert.NotNull(result);
        Assert.Equal(SampleTemplate.Name, result.Name);
        Assert.Equal("Draft", result.Status);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SetsVersion1AndStatusDraft()
    {
        _templateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FieldMappingTemplate>()))
            .ReturnsAsync((FieldMappingTemplate t) => t);

        var request = new CreateTemplateRequest
        {
            Name = "New Template",
            TmsSystemId = SampleTemplate.TmsSystemId
        };

        var result = await _sut.CreateAsync(request);

        Assert.Equal(1, result.Version);
        Assert.Equal("Draft", result.Status);
        Assert.Equal("New Template", result.Name);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenTemplateNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(It.IsAny<Guid>()))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateTemplateRequest());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_IncrementsVersion()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(SampleTemplate.TemplateId))
            .ReturnsAsync(SampleTemplate);
        _templateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FieldMappingTemplate>()))
            .ReturnsAsync((FieldMappingTemplate t) => t);

        var result = await _sut.UpdateAsync(SampleTemplate.TemplateId, new UpdateTemplateRequest
        {
            Name = "Updated Name"
        });

        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.Equal("Updated Name", result.Name);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(It.IsAny<Guid>()))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        _templateRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepo_AndReturnsTrue_WhenFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(SampleTemplate.TemplateId))
            .ReturnsAsync(SampleTemplate);

        var result = await _sut.DeleteAsync(SampleTemplate.TemplateId);

        Assert.True(result);
        _templateRepoMock.Verify(r => r.DeleteAsync(SampleTemplate.TemplateId, null), Times.Once);
    }

    // ── DuplicateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DuplicateAsync_ReturnsNull_WhenSourceNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(It.IsAny<Guid>()))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.DuplicateAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task DuplicateAsync_CreatesNewTemplateWithCopySuffix_AndCopiesMappings()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(SampleTemplate.TemplateId))
            .ReturnsAsync(SampleTemplate);

        FieldMappingTemplate? savedTemplate = null;
        _templateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FieldMappingTemplate>()))
            .Callback<FieldMappingTemplate>(t => savedTemplate = t)
            .ReturnsAsync((FieldMappingTemplate t) => t);

        var sourceMappings = new List<FieldMapping>
        {
            new() { Id = Guid.NewGuid(), SourcePath = "a", TargetPath = "b", TemplateVersionId = SampleTemplate.Id }
        };
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(SampleTemplate.Id))
            .ReturnsAsync(sourceMappings);
        _mappingRepoMock
            .Setup(r => r.CreateBulkAsync(It.IsAny<List<FieldMapping>>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DuplicateAsync(SampleTemplate.TemplateId);

        Assert.NotNull(result);
        Assert.Contains("Copy", result.Name);
        Assert.Equal(1, result.Version);
        _mappingRepoMock.Verify(r => r.CreateBulkAsync(It.Is<List<FieldMapping>>(
            list => list.Count == 1 && list[0].SourcePath == "a")), Times.Once);
    }
}
