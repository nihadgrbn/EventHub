using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Tickets.Commands.CheckInTicket;
using EventHub.Application.Tickets.Queries.GetTicketQr;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Tickets;

public sealed class CheckInTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ISecureTokenService> _secureTokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_ValidQrForOrganizer_ChecksInTicketAndReturnsDetails()
    {
        var organizerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        var token = $"  {new string('a', 64)}  "; // Lowercase + whitespace to verify normalization
        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);
        _tickets.Setup(repository => repository.GetByQrTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ticket
            {
                Id = ticketId,
                EventId = eventId,
                AttendeeId = attendeeId,
                TicketTypeNameAtPurchase = "VIP",
                PriceAtPurchase = 150m,
                Event = new Event { Id = eventId, Title = "Baku Tech Fest", OrganizerId = organizerId, Status = EventStatus.Published },
                Attendee = new User { Id = attendeeId, FirstName = "Ali", LastName = "Aliyev", Email = "ali@example.com" }
            });
        _tickets.Setup(repository => repository.TryCheckInAsync(eventId, It.IsAny<string>(), organizerId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await new CheckInTicketCommandHandler(_tickets.Object, _currentUser.Object)
            .Handle(new CheckInTicketCommand(eventId, token), CancellationToken.None);

        Assert.Equal(ticketId, response.TicketId);
        Assert.Equal(eventId, response.EventId);
        Assert.Equal("Baku Tech Fest", response.EventTitle);
        Assert.Equal("VIP", response.TicketTypeName);
        Assert.Equal(150m, response.PriceAtPurchase);
        Assert.Equal("Ali Aliyev", response.AttendeeName);
        Assert.Equal("ali@example.com", response.AttendeeEmail);
        _tickets.Verify(repository => repository.TryCheckInAsync(eventId, It.IsAny<string>(), organizerId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTicketBelongsToDifferentEventOfSameOrganizer_ThrowsConflictWithEventTitle()
    {
        var organizerId = Guid.NewGuid();
        var scannedForEventId = Guid.NewGuid();
        var actualTicketEventId = Guid.NewGuid();
        var token = new string('B', 64);

        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);
        _tickets.Setup(repository => repository.GetByQrTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ticket
            {
                EventId = actualTicketEventId,
                Event = new Event { Id = actualTicketEventId, Title = "Other Concert", OrganizerId = organizerId, Status = EventStatus.Published }
            });

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            new CheckInTicketCommandHandler(_tickets.Object, _currentUser.Object)
                .Handle(new CheckInTicketCommand(scannedForEventId, token), CancellationToken.None));

        Assert.Contains("Other Concert", exception.Message);
    }

    [Fact]
    public async Task GetTicketQr_MultipleCalls_ReturnsConsistentDeterministicQrTokenWithoutOverwritingValidHash()
    {
        var attendeeId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        const string deterministicToken = "DETERMINISTIC_QR_TOKEN";
        const string deterministicHash = "DETERMINISTIC_QR_HASH";

        var ticket = new Ticket
        {
            Id = ticketId,
            AttendeeId = attendeeId,
            Status = TicketStatus.Active,
            QrTokenHash = deterministicHash,
            Event = new Event { Status = EventStatus.Published }
        };

        _currentUser.SetupGet(service => service.UserId).Returns(attendeeId);
        _tickets.Setup(repository => repository.GetTicketByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _secureTokenService.Setup(service => service.GenerateTicketQrToken(ticketId))
            .Returns(deterministicToken);
        _secureTokenService.Setup(service => service.HashToken(deterministicToken))
            .Returns(deterministicHash);

        var handler = new GetTicketQrQueryHandler(
            _tickets.Object,
            _currentUser.Object,
            _secureTokenService.Object,
            _unitOfWork.Object);

        var firstResult = await handler.Handle(new GetTicketQrQuery(ticketId), CancellationToken.None);
        var secondResult = await handler.Handle(new GetTicketQrQuery(ticketId), CancellationToken.None);

        Assert.Equal(deterministicToken, firstResult.Payload);
        Assert.Equal(firstResult.Payload, secondResult.Payload);
        Assert.Equal(deterministicHash, ticket.QrTokenHash);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
