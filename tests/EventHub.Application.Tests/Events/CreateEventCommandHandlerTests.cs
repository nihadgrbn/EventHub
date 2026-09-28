using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Commands.CreateEvent;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Events;

public sealed class CreateEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private CreateEventCommandHandler CreateHandler() =>
        new(_events.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_UserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var handler = CreateHandler();
        var command = new CreateEventCommand(
            "Title",
            "Description",
            DateTime.UtcNow.AddDays(5),
            "Baku",
            EventCategory.Concert,
            "Nizami St",
            null,
            null,
            null,
            []);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesEventWithDraftStatusAndTicketTypes()
    {
        var organizerId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(organizerId);

        Event? addedEvent = null;
        _events.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => addedEvent = e)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var eventDate = DateTime.UtcNow.AddDays(10);
        var command = new CreateEventCommand(
            "Rock Concert",
            "Awesome live rock concert",
            eventDate,
            "Baku",
            EventCategory.Concert,
            "Heydar Aliyev Palace",
            "http://localhost:8080/uploads/posters/rock.jpg",
            40.4093m,
            49.8671m,
            [
                new CreateTicketTypeDto("VIP", 150m, 50),
                new CreateTicketTypeDto("Standard", 50m, 200)
            ]);

        var eventId = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, eventId);
        Assert.NotNull(addedEvent);
        Assert.Equal("Rock Concert", addedEvent.Title);
        Assert.Equal(organizerId, addedEvent.OrganizerId);
        Assert.Equal(EventStatus.Draft, addedEvent.Status);
        Assert.Equal(2, addedEvent.TicketTypes.Count);
        Assert.Equal(50, addedEvent.TicketTypes.ElementAt(0).AvailableQuantity);
        Assert.Equal(200, addedEvent.TicketTypes.ElementAt(1).AvailableQuantity);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
