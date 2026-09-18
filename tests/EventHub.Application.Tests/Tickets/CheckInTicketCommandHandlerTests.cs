using EventHub.Application.Common.Interfaces;
using EventHub.Application.Tickets.Commands.CheckInTicket;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Tickets;

public sealed class CheckInTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public async Task Handle_ValidQrForOrganizer_ChecksInTicket()
    {
        var organizerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var token = new string('A', 64);
        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);
        _tickets.Setup(repository => repository.GetByQrTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ticket
            {
                EventId = eventId,
                Event = new Event { OrganizerId = organizerId, Status = EventStatus.Published }
            });
        _tickets.Setup(repository => repository.TryCheckInAsync(eventId, It.IsAny<string>(), organizerId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await new CheckInTicketCommandHandler(_tickets.Object, _currentUser.Object)
            .Handle(new CheckInTicketCommand(eventId, token), CancellationToken.None);

        _tickets.Verify(repository => repository.TryCheckInAsync(eventId, It.IsAny<string>(), organizerId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
