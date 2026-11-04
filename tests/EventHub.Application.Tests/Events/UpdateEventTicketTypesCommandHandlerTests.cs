using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Commands.UpdateTicketTypes;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Events;

public sealed class UpdateEventTicketTypesCommandHandlerTests
{
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IReservationRepository> _reservations = new();

    [Fact]
    public async Task Handle_CannotDeleteTicketTypeThatHasSales()
    {
        var organizerId = Guid.NewGuid();
        var ticketType = new TicketType { Name = "VIP", Price = 50m, Quantity = 10, AvailableQuantity = 8 };
        var @event = new Event { OrganizerId = organizerId, TicketTypes = [ticketType] };
        _events.Setup(repository => repository.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _tickets.Setup(repository => repository.GetCountsByTicketTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [ticketType.Id] = 2 });
        _reservations.Setup(repository => repository.GetActiveQuantitiesByTicketTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new UpdateEventTicketTypesCommand(@event.Id, []), CancellationToken.None));

        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CannotDeleteTicketTypeThatHasActiveReservations()
    {
        var organizerId = Guid.NewGuid();
        var ticketType = new TicketType { Name = "VIP", Price = 50m, Quantity = 10, AvailableQuantity = 7 };
        var @event = new Event { OrganizerId = organizerId, Status = EventStatus.Published, TicketTypes = [ticketType] };
        _events.Setup(repository => repository.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _tickets.Setup(repository => repository.GetCountsByTicketTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        _reservations.Setup(repository => repository.GetActiveQuantitiesByTicketTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [ticketType.Id] = 3 });
        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new UpdateEventTicketTypesCommand(@event.Id, []), CancellationToken.None));

        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIncreasingQuantityOnPublishedEvent_PreservesSoldAndActiveReservedInventory()
    {
        var organizerId = Guid.NewGuid();
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            Name = "Standard",
            Price = 25m,
            Quantity = 100,
            AvailableQuantity = 85 // 10 sold + 5 active reserved = 15 committed
        };
        var @event = new Event
        {
            OrganizerId = organizerId,
            Status = EventStatus.Published,
            TicketTypes = [ticketType]
        };

        _events.Setup(repository => repository.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _tickets.Setup(repository => repository.GetCountsByTicketTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [ticketType.Id] = 10 });
        _reservations.Setup(repository => repository.GetActiveQuantitiesByTicketTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [ticketType.Id] = 5 });
        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);

        await CreateHandler().Handle(
            new UpdateEventTicketTypesCommand(
                @event.Id,
                [new UpdateTicketTypeDto(ticketType.Id, "Standard", 25m, 120)]),
            CancellationToken.None);

        Assert.Equal(120, ticketType.Quantity);
        Assert.Equal(105, ticketType.AvailableQuantity); // 120 - (10 sold + 5 reserved) = 105
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private UpdateEventTicketTypesCommandHandler CreateHandler() =>
        new(_events.Object, _tickets.Object, _unitOfWork.Object, _currentUser.Object, _reservations.Object);
}
