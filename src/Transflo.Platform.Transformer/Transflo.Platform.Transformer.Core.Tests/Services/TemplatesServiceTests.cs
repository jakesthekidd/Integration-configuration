using Moq;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.Interfaces;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class TemplatesServiceTests
{
    private readonly Mock<ITemplateRepository> _templateRepoMock = new();
    private readonly Mock<IFieldMappingRepository> _mappingRepoMock = new();
    private readonly Mock<ITemplateVersionRepository> _versionRepoMock = new();
    private readonly Mock<IFieldMappingValidationService> _validationServiceMock = new();
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

    public TemplatesServiceTests()
    {
        _sut = new TemplatesService(
            _templateRepoMock.Object,
            _mappingRepoMock.Object,
            _versionRepoMock.Object,
            _validationServiceMock.Object);
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
    public async Task CreateAsync_SetsStatusActiveAndCreatesVersion1AsDraft()
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

        Assert.Equal("Active", result.Status);
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
    public async Task DuplicateAsync_Default_IncludeAllVersions_CopiesAllVersions()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleTemplate);

        _templateRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Template>());

        var v1 = new TemplateVersion { Id = Guid.NewGuid(), TemplateId = SampleTemplate.Id, Version = 1, Status = TemplateVersionStatus.Superseded };
        var v2 = new TemplateVersion { Id = Guid.NewGuid(), TemplateId = SampleTemplate.Id, Version = 2, Status = TemplateVersionStatus.Published };

        _versionRepoMock
            .Setup(r => r.GetAllVersionsAsync(SampleTemplate.Id))
            .ReturnsAsync(new List<TemplateVersion> { v1, v2 });

        _templateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Template>()))
            .ReturnsAsync((Template t) => { t.Id = Guid.NewGuid(); return t; });

        _versionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TemplateVersion>()))
            .ReturnsAsync((TemplateVersion v) => { v.Id = Guid.NewGuid(); return v; });

        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<FieldMapping>());

        var result = await _sut.DuplicateAsync(SampleTemplate.Id);

        Assert.NotNull(result);
        Assert.Equal($"{SampleTemplate.Name} - Copy", result.Name);
        _versionRepoMock.Verify(r => r.CreateAsync(It.Is<TemplateVersion>(v => v.Version == 1)), Times.Once);
        _versionRepoMock.Verify(r => r.CreateAsync(It.Is<TemplateVersion>(v => v.Version == 2)), Times.Once);
    }

    [Fact]
    public async Task DuplicateAsync_WithLatestVersionOnly_CopiesOnlyLatestToV1()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleTemplate);

        _templateRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Template>());

        var v1 = new TemplateVersion { Id = Guid.NewGuid(), TemplateId = SampleTemplate.Id, Version = 1, Status = TemplateVersionStatus.Superseded };
        var v2 = new TemplateVersion { Id = Guid.NewGuid(), TemplateId = SampleTemplate.Id, Version = 2, Status = TemplateVersionStatus.Published };

        _versionRepoMock
            .Setup(r => r.GetAllVersionsAsync(SampleTemplate.Id))
            .ReturnsAsync(new List<TemplateVersion> { v1, v2 });

        _templateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Template>()))
            .ReturnsAsync((Template t) => { t.Id = Guid.NewGuid(); return t; });

        _versionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TemplateVersion>()))
            .ReturnsAsync((TemplateVersion v) => { v.Id = Guid.NewGuid(); return v; });

        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(v2.Id))
            .ReturnsAsync(new List<FieldMapping> { new() { SourcePath = "src", TargetPath = "tgt" } });

        var result = await _sut.DuplicateAsync(SampleTemplate.Id, new DuplicateTemplateRequest { IncludeAllVersions = false });

        Assert.NotNull(result);
        Assert.Equal($"{SampleTemplate.Name} - Copy", result.Name);
        _versionRepoMock.Verify(r => r.CreateAsync(It.Is<TemplateVersion>(v => v.Version == 1 && v.Status == TemplateVersionStatus.Draft)), Times.Once);
        _versionRepoMock.Verify(r => r.CreateAsync(It.Is<TemplateVersion>(v => v.Version != 1)), Times.Never);
        _mappingRepoMock.Verify(r => r.CreateBulkAsync(It.Is<List<FieldMapping>>(l => l.Count == 1)), Times.Once);
    }

    [Fact]
    public async Task DuplicateAsync_WithExistingCopy_AppendsCounter()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleTemplate);

        var existingCopy = new Template { Name = $"{SampleTemplate.Name} - Copy" };
        var existingCopy1 = new Template { Name = $"{SampleTemplate.Name} - Copy 1" };
        _templateRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Template> { existingCopy, existingCopy1 });

        _templateRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Template>()))
            .ReturnsAsync((Template t) => { t.Id = Guid.NewGuid(); return t; });

        _versionRepoMock
            .Setup(r => r.GetAllVersionsAsync(SampleTemplate.Id))
            .ReturnsAsync(new List<TemplateVersion>());

        var result = await _sut.DuplicateAsync(SampleTemplate.Id);

        Assert.NotNull(result);
        Assert.Equal($"{SampleTemplate.Name} - Copy 2", result.Name);
    }

    [Fact]
    public async Task CreateVersionAsync_SetsBaseVersion_AndCopiesMappings()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(SampleTemplate.Id))
            .ReturnsAsync(SampleTemplate);

        var v1 = new TemplateVersion { Id = Guid.NewGuid(), TemplateId = SampleTemplate.Id, Version = 1, Status = TemplateVersionStatus.Published };
        _versionRepoMock
            .Setup(r => r.GetPublishedVersionAsync(SampleTemplate.Id))
            .ReturnsAsync(v1);
        _versionRepoMock
            .Setup(r => r.GetAllVersionsAsync(SampleTemplate.Id))
            .ReturnsAsync(new List<TemplateVersion> { v1 });

        _versionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TemplateVersion>()))
            .ReturnsAsync((TemplateVersion v) => { v.Id = Guid.NewGuid(); return v; });

        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(v1.Id))
            .ReturnsAsync(new List<FieldMapping> { new() { SourcePath = "s" } });

        var result = await _sut.CreateVersionAsync(SampleTemplate.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.Equal(1, result.BaseVersion);
        _mappingRepoMock.Verify(r => r.CreateBulkAsync(It.Is<List<FieldMapping>>(l => l.Count == 1)), Times.Once);
    }

    // ── PublishVersionAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task PublishVersionAsync_ReturnsNull_WhenVersionNotFound()
    {
        _versionRepoMock
            .Setup(r => r.GetByVersionAsync(SampleTemplate.Id, 1))
            .ReturnsAsync((TemplateVersion?)null);

        var result = await _sut.PublishVersionAsync(SampleTemplate.Id, 1);

        Assert.Null(result);
        _validationServiceMock.Verify(v => v.ValidateAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PublishVersionAsync_ReturnsNull_WhenVersionIsNotDraft()
    {
        var published = new TemplateVersion { Id = Guid.NewGuid(), TemplateId = SampleTemplate.Id, Version = 1, Status = TemplateVersionStatus.Published };
        _versionRepoMock
            .Setup(r => r.GetByVersionAsync(SampleTemplate.Id, 1))
            .ReturnsAsync(published);

        var result = await _sut.PublishVersionAsync(SampleTemplate.Id, 1);

        Assert.Null(result);
        _validationServiceMock.Verify(v => v.ValidateAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PublishVersionAsync_ReturnsNull_WhenValidationFails()
    {
        var draft = new TemplateVersion { Id = Guid.NewGuid(), TemplateId = SampleTemplate.Id, Version = 1, Status = TemplateVersionStatus.Draft };
        _versionRepoMock
            .Setup(r => r.GetByVersionAsync(SampleTemplate.Id, 1))
            .ReturnsAsync(draft);

        _validationServiceMock
            .Setup(v => v.ValidateAsync(SampleTemplate.Id, 1))
            .ReturnsAsync(new MappingValidationResult
            {
                IsValid = false,
                Issues = [new ValidationIssue { Severity = ValidationSeverity.Error, Code = "MISSING_TARGET_PATH", Message = "TargetPath is required." }]
            });

        var result = await _sut.PublishVersionAsync(SampleTemplate.Id, 1);

        Assert.Null(result);
        _versionRepoMock.Verify(r => r.PublishVersionAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PublishVersionAsync_ReturnsPublishedVersion_WhenValidationPasses()
    {
        var draft = new TemplateVersion { Id = Guid.NewGuid(), TemplateId = SampleTemplate.Id, Version = 1, Status = TemplateVersionStatus.Draft };
        var publishedVersion = new TemplateVersion { Id = draft.Id, TemplateId = SampleTemplate.Id, Version = 1, Status = TemplateVersionStatus.Published, PublishedBy = "user1" };

        _versionRepoMock
            .Setup(r => r.GetByVersionAsync(SampleTemplate.Id, 1))
            .ReturnsAsync(draft);

        _validationServiceMock
            .Setup(v => v.ValidateAsync(SampleTemplate.Id, 1))
            .ReturnsAsync(new MappingValidationResult { IsValid = true, Issues = [] });

        _versionRepoMock
            .Setup(r => r.PublishVersionAsync(SampleTemplate.Id, 1, "user1"))
            .ReturnsAsync(publishedVersion);

        var result = await _sut.PublishVersionAsync(SampleTemplate.Id, 1, "user1");

        Assert.NotNull(result);
        Assert.Equal("Published", result.Status);
        _versionRepoMock.Verify(r => r.PublishVersionAsync(SampleTemplate.Id, 1, "user1"), Times.Once);
    }
}
