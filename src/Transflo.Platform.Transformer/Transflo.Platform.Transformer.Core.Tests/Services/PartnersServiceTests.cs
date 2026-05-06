using Moq;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class PartnersServiceTests
{
    private readonly Mock<IPartnerRepository> _repoMock = new();
    private readonly PartnersService _sut;

    private static readonly Guid PartnerId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static readonly Partner SamplePartner = new()
    {
        Id = PartnerId,
        Name = "Test Partner",
        Description = "Test partner Description",
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        IsDeleted = false
    };

    public PartnersServiceTests()
    {
        _sut = new PartnersService(_repoMock.Object);
    }


    [Fact]
    public async Task GetAllAsync_ReturnsMappedItems_WhenRepoHasData()
    {
        _repoMock
            .Setup(r => r.GetAllAsync(1, 20))
            .ReturnsAsync((new List<Partner> { SamplePartner }, 1));

        var (items, totalCount) = await _sut.GetAllAsync(1, 20);

        Assert.Single(items);
        Assert.Equal(SamplePartner.Id, items[0].Id);
        Assert.Equal(SamplePartner.Name, items[0].Name);
        Assert.Equal(SamplePartner.Description, items[0].Description);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenPageExceedsTotal()
    {
        _repoMock
            .Setup(r => r.GetAllAsync(99, 20))
            .ReturnsAsync((new List<Partner>(), 1));

        var (items, totalCount) = await _sut.GetAllAsync(99, 20);

        Assert.Empty(items);
        Assert.Equal(1, totalCount);
    }


    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenPartnerNotFound()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Partner?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedResponse_WhenFound()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(PartnerId))
            .ReturnsAsync(SamplePartner);

        var result = await _sut.GetByIdAsync(PartnerId);

        Assert.NotNull(result);
        Assert.Equal(SamplePartner.Id, result.Id);
        Assert.Equal(SamplePartner.Name, result.Name);
        Assert.Equal(SamplePartner.Description, result.Description);
        Assert.Equal(SamplePartner.CreatedAt, result.CreatedAt);
        Assert.Equal(SamplePartner.UpdatedAt, result.UpdatedAt);
    }


    [Fact]
    public async Task CreateAsync_CallsRepo_AndReturnsMappedResponse()
    {
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Partner>()))
            .ReturnsAsync((Partner p) => p);

        var request = new CreatePartnerRequest { Name = "New Partner", Description = "desc" };

        var result = await _sut.CreateAsync(request);

        _repoMock.Verify(r => r.CreateAsync(It.Is<Partner>(p =>
            p.Name == "New Partner" &&
            p.Description == "desc" &&
            p.Id != Guid.Empty)), Times.Once);

        Assert.Equal("New Partner", result.Name);
        Assert.Equal("desc", result.Description);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateAsync_AssignsNewGuid_EachCall()
    {
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Partner>()))
            .ReturnsAsync((Partner p) => p);

        var r1 = await _sut.CreateAsync(new CreatePartnerRequest { Name = "P1" });
        var r2 = await _sut.CreateAsync(new CreatePartnerRequest { Name = "P2" });

        Assert.NotEqual(r1.Id, r2.Id);
    }


    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenPartnerNotFound()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Partner?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdatePartnerRequest { Name = "X" });

        Assert.Null(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Partner>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_AndReturnsMappedResponse()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(PartnerId))
            .ReturnsAsync(SamplePartner);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Partner>()))
            .ReturnsAsync((Partner p) => p);

        var result = await _sut.UpdateAsync(PartnerId, new UpdatePartnerRequest
        {
            Name = "Updated Name",
            Description = "Updated desc"
        });

        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated desc", result.Description);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Partner>(p =>
            p.Name == "Updated Name" &&
            p.Description == "Updated desc")), Times.Once);
    }


    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenPartnerNotFound()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Partner?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepoDelete_AndReturnsTrue_WhenFound()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(PartnerId))
            .ReturnsAsync(SamplePartner);
        _repoMock
            .Setup(r => r.DeleteAsync(PartnerId))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(PartnerId);

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(PartnerId), Times.Once);
    }
}
