using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Commands.DeletePoster;
using EventHub.Application.Events.Commands.UploadPoster;
using EventHub.Domain.Constants;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Events;

public sealed class UploadEventPosterCommandHandlerTests
{
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IFileStorageService> _fileStorage = new();

    [Fact]
    public async Task Handle_ValidRequestByEventOrganizer_SavesPosterAndUpdatesEvent()
    {
        var organizerId = Guid.NewGuid();
        var @event = new Event
        {
            OrganizerId = organizerId,
            Status = EventStatus.Draft,
            PosterImageUrl = "http://localhost/uploads/posters/old.jpg"
        };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(s => s.UserId).Returns(organizerId);
        _currentUser.Setup(s => s.IsInRole(Roles.Admin)).Returns(false);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var expectedUrl = "http://localhost/uploads/posters/new.jpg";
        _fileStorage.Setup(s => s.SavePosterAsync(stream, "poster.jpg", "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var handler = CreateUploadHandler();
        var result = await handler.Handle(
            new UploadEventPosterCommand(@event.Id, stream, "poster.jpg", "image/jpeg", stream.Length),
            CancellationToken.None);

        Assert.Equal(expectedUrl, result);
        Assert.Equal(expectedUrl, @event.PosterImageUrl);
        _fileStorage.Verify(s => s.DeleteFileAsync("http://localhost/uploads/posters/old.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(r => r.Update(@event), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminCanUploadPosterForAnyEvent()
    {
        var adminId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = new Event
        {
            OrganizerId = organizerId,
            Status = EventStatus.Published
        };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(s => s.UserId).Returns(adminId);
        _currentUser.Setup(s => s.IsInRole(Roles.Admin)).Returns(true);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var expectedUrl = "http://localhost/uploads/posters/admin-upload.png";
        _fileStorage.Setup(s => s.SavePosterAsync(stream, "poster.png", "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var handler = CreateUploadHandler();
        var result = await handler.Handle(
            new UploadEventPosterCommand(@event.Id, stream, "poster.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.Equal(expectedUrl, result);
        Assert.Equal(expectedUrl, @event.PosterImageUrl);
    }

    [Fact]
    public async Task Handle_EventNotFound_ThrowsNotFoundException()
    {
        _events.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var handler = CreateUploadHandler();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UploadEventPosterCommand(Guid.NewGuid(), stream, "poster.jpg", "image/jpeg", 10), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedException()
    {
        var @event = new Event { OrganizerId = Guid.NewGuid(), Status = EventStatus.Draft };
        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(s => s.UserId).Returns((Guid?)null);

        var handler = CreateUploadHandler();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new UploadEventPosterCommand(@event.Id, stream, "poster.jpg", "image/jpeg", 10), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DifferentUserNotAdmin_ThrowsForbiddenException()
    {
        var organizerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var @event = new Event { OrganizerId = organizerId, Status = EventStatus.Draft };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(s => s.UserId).Returns(differentUserId);
        _currentUser.Setup(s => s.IsInRole(Roles.Admin)).Returns(false);

        var handler = CreateUploadHandler();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UploadEventPosterCommand(@event.Id, stream, "poster.jpg", "image/jpeg", 10), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CancelledOrCompletedEvent_ThrowsConflictException()
    {
        var organizerId = Guid.NewGuid();
        var @event = new Event { OrganizerId = organizerId, Status = EventStatus.Cancelled };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(s => s.UserId).Returns(organizerId);

        var handler = CreateUploadHandler();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UploadEventPosterCommand(@event.Id, stream, "poster.jpg", "image/jpeg", 10), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeletePosterCommand_DeletesFileAndClearsUrl()
    {
        var organizerId = Guid.NewGuid();
        var @event = new Event
        {
            OrganizerId = organizerId,
            Status = EventStatus.Draft,
            PosterImageUrl = "http://localhost/uploads/posters/existing.png"
        };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(s => s.UserId).Returns(organizerId);
        _currentUser.Setup(s => s.IsInRole(Roles.Admin)).Returns(false);

        var deleteHandler = new DeleteEventPosterCommandHandler(
            _events.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _fileStorage.Object);

        await deleteHandler.Handle(new DeleteEventPosterCommand(@event.Id), CancellationToken.None);

        Assert.Null(@event.PosterImageUrl);
        _fileStorage.Verify(s => s.DeleteFileAsync("http://localhost/uploads/posters/existing.png", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private UploadEventPosterCommandHandler CreateUploadHandler() =>
        new(_events.Object, _unitOfWork.Object, _currentUser.Object, _fileStorage.Object);
}
