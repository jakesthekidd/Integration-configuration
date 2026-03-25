using Moq;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class TemplatesServiceTests
{
    private readonly Mock<ITemplateRepository> _templateRepoMock = new();
    private readonly Mock<IFieldMappingRepository> _mappingRepoMock = new();
    private readonly Mock<ITemplateVersionRepository> _versionRepoMock = new();
    private readonly TemplatesService _sut;

    private static readonly Template SampleTemplate = new()
    {
        Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
        Name = "Test Template",
        Description = "Desc",
        Status = TemplateStatus.Draft,
        SourceSchema = "Source",
        TargetSchema = "Target"
    };

    private static readonly TemplateVersion SampleVersion = new()
    {
        Id = Guid.NewGuid(),
        TemplateId = SampleTemplate.Id,
        Version = 1,
        Status = TemplateVersionStatus.Published
    };

    public TemplatesServiceTests()
    {
        _sut = new TemplatesService(_templateRepoMock.Object, _mappingRepoMock.Object, _versionRepoMock.Object);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_CallsGetAllAsync()
    {
        _templateRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Template> { SampleTemplate });

        var result = await _sut.GetAllAsync();

        _templateRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        Assert.Single(result);
        Assert.Equal(SampleTemplate.Id, result[0].Id);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Template?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsResponse_WhenFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleTemplate);

        var result = await _sut.GetByIdAsync(SampleTemplate.Id);

        Assert.NotNull(result);
        Assert.Equal(SampleTemplate.Name, result.Name);
        Assert.Equal("Draft", result.Status);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SetsStatusDraftAndCreatesVersion1()
    {
        _templateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Template>()))
            .ReturnsAsync((Template t) => t);
            
        _versionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TemplateVersion>()))
            .ReturnsAsync((TemplateVersion v) => v);

        var request = new CreateTemplateRequest
        {
            Name = "New Template"
        };

        var result = await _sut.CreateAsync(request);

        _versionRepoMock.Verify(r => r.CreateAsync(It.Is<TemplateVersion>(v => v.Version == 1 && v.Status == TemplateVersionStatus.Draft)), Times.Once);
        
        Assert.Equal("Draft", result.Status);
        Assert.Equal("New Template", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenTemplateNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Template?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateTemplateRequest());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTemplate()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleTemplate);
        _templateRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Template>()))
            .ReturnsAsync((Template t) => t);

        var result = await _sut.UpdateAsync(SampleTemplate.Id, new UpdateTemplateRequest
        {
            Name = "Updated Name"
        });

        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Template?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        _templateRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepo_AndReturnsTrue_WhenFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleTemplate);

        var result = await _sut.DeleteAsync(SampleTemplate.Id);

        Assert.True(result);
        _templateRepoMock.Verify(r => r.DeleteAsync(SampleTemplate.Id), Times.Once);
    }

    // ── DuplicateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DuplicateAsync_ReturnsNull_WhenSourceNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Template?)null);

        var result = await _sut.DuplicateAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task DuplicateAsync_CreatesNewTemplateWithCopySuffix_AndCopiesMappings()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleTemplate);
            
        _versionRepoMock
            .Setup(r => r.GetPublishedVersionAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleVersion);

        Template? savedTemplate = null;
        _templateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Template>()))
            .Callback<Template>(t => savedTemplate = t)
            .ReturnsAsync((Template t) => t);
            
        TemplateVersion? savedVersion = null;
        _versionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TemplateVersion>()))
            .Callback<TemplateVersion>(v => savedVersion = v)
            .ReturnsAsync((TemplateVersion v) => v);

        var sourceMappings = new List<FieldMapping>
        {
            new() { Id = Guid.NewGuid(), SourcePath = "a", TargetPath = "b", TemplateVersionId = SampleVersion.Id }
        };
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(SampleVersion.Id))
            .ReturnsAsync(sourceMappings);
        _mappingRepoMock
            .Setup(r => r.CreateBulkAsync(It.IsAny<List<FieldMapping>>()))
            .ReturnsAsync(sourceMappings);

        var result = await _sut.DuplicateAsync(SampleTemplate.Id);

        Assert.NotNull(result);
        Assert.Contains("Copy", result.Name);
        _mappingRepoMock.Verify(r => r.CreateBulkAsync(It.Is<List<FieldMapping>>(
            list => list.Count == 1 && list[0].SourcePath == "a")), Times.Once);
    }
}
