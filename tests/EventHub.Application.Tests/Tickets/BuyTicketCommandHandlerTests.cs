using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Tickets.Commands.BuyTicket;
using EventHub.Domain.Entities;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Tickets;

public sealed class BuyTicketCommandHandlerTests
{
    private readonly Mock<ITicketTypeRepository> _ticketTypes = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IPurchaseRepository> _purchases = new();
    private readonly Mock<IOutboxRepository> _outbox = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IEventRepository> _events = new();

    [Fact]
    public async Task Handle_WhenStockIsAvailable_CreatesPurchaseTicketsAndOutboxMessage()
    {
        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        var attendee = new User { Id = attendeeId, FirstName = "Test", LastName = "User", Email = "test@example.com" };
        var eventEntity = new Event { Id = eventId, Title = "Test event", Date = DateTime.UtcNow.AddDays(1) };
        var ticketType = new TicketType { Id = ticketTypeId, EventId = eventId, Name = "Standard", Price = 25m };

        _currentUser.SetupGet(service => service.UserId).Returns(attendeeId);
        _ticketTypes.Setup(repository => repository.GetByIdAsync(ticketTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(ticketType);
        _users.Setup(repository => repository.GetByIdAsync(attendeeId, It.IsAny<CancellationToken>())).ReturnsAsync(attendee);
        _events.Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(eventEntity);
        _ticketTypes.Setup(repository => repository.TryDecreaseAvailableQuantityAsync(ticketTypeId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWork.Setup(unit => unit.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, token) => action(token));

        var handler = CreateHandler();
        var result = await handler.Handle(new BuyTicketCommand(eventId, ticketTypeId, 2), CancellationToken.None);

        Assert.Equal(2, result.Count);
        _purchases.Verify(repository => repository.AddAsync(It.Is<Purchase>(purchase => purchase.TotalAmount == 50m && purchase.Tickets.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
        _outbox.Verify(repository => repository.AddAsync(It.Is<OutboxMessage>(message => message.Type == "PurchaseReceiptEvent" && message.Payload.Contains("Test event")), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUser.SetupGet(service => service.UserId).Returns((Guid?)null);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new BuyTicketCommand(Guid.NewGuid(), Guid.NewGuid(), 1), CancellationToken.None));

        Assert.Equal("You must be logged in to buy a ticket.", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenTicketTypeDoesNotExist_ThrowsNotFoundException()
    {
        var attendeeId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        _currentUser.SetupGet(service => service.UserId).Returns(attendeeId);
        _ticketTypes.Setup(repository => repository.GetByIdAsync(ticketTypeId, It.IsAny<CancellationToken>())).ReturnsAsync((TicketType?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new BuyTicketCommand(Guid.NewGuid(), ticketTypeId, 1), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenStockIsUnavailable_ThrowsConflictException()
    {
        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        _currentUser.SetupGet(service => service.UserId).Returns(attendeeId);
        _ticketTypes.Setup(repository => repository.GetByIdAsync(ticketTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(new TicketType { Id = ticketTypeId, EventId = eventId, Price = 10m });
        _users.Setup(repository => repository.GetByIdAsync(attendeeId, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = attendeeId });
        _events.Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(new Event { Id = eventId });
        _ticketTypes.Setup(repository => repository.TryDecreaseAvailableQuantityAsync(ticketTypeId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWork.Setup(unit => unit.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, token) => action(token));

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new BuyTicketCommand(eventId, ticketTypeId, 1), CancellationToken.None));
    }

    private BuyTicketCommandHandler CreateHandler()
    {
        return new BuyTicketCommandHandler(
            _ticketTypes.Object,
            _tickets.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _purchases.Object,
            _outbox.Object,
            _users.Object,
            _events.Object);
    }
}
