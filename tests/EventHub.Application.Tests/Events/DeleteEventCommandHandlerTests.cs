using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Commands.DeleteEvent;
using EventHub.Domain.Constants;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Events;

public sealed class DeleteEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IFileStorageService> _fileStorage = new();

    private DeleteEventCommandHandler CreateHandler() =>
        new(_events.Object, _unitOfWork.Object, _currentUser.Object, _tickets.Object, _fileStorage.Object);

    [Fact]
    public async Task Handle_EventNotFound_ThrowsNotFoundException()
    {
        _events.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteEventCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserNotOwnerNorAdmin_ThrowsForbiddenException()
    {
        var organizerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var @event = new Event { Id = Guid.NewGuid(), OrganizerId = organizerId, Status = EventStatus.Draft };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(c => c.UserId).Returns(anotherUserId);
        _currentUser.Setup(c => c.IsInRole(Roles.Admin)).Returns(false);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteEventCommand(@event.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EventNotDraft_ThrowsConflictException()
    {
        var organizerId = Guid.NewGuid();
        var @event = new Event { Id = Guid.NewGuid(), OrganizerId = organizerId, Status = EventStatus.Published };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(c => c.UserId).Returns(organizerId);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DeleteEventCommand(@event.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DraftEventWithSoldTickets_ThrowsConflictException()
    {
        var organizerId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerId,
            Status = EventStatus.Draft,
            TicketTypes = [new TicketType { Id = ticketTypeId, Name = "Standard", Quantity = 10 }]
        };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(c => c.UserId).Returns(organizerId);
        _tickets.Setup(t => t.GetCountsByTicketTypeIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { ticketTypeId, 2 } });

        var handler = CreateHandler();
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DeleteEventCommand(@event.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidDraftEvent_DeletesEventAndCleansPosterFile()
    {
        var organizerId = Guid.NewGuid();
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerId,
            Status = EventStatus.Draft,
            PosterImageUrl = "http://localhost:8080/uploads/posters/poster.jpg",
            TicketTypes = []
        };

        _events.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(c => c.UserId).Returns(organizerId);
        _tickets.Setup(t => t.GetCountsByTicketTypeIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var handler = CreateHandler();
        await handler.Handle(new DeleteEventCommand(@event.Id), CancellationToken.None);

        _fileStorage.Verify(f => f.DeleteFileAsync("http://localhost:8080/uploads/posters/poster.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(r => r.Delete(@event), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
