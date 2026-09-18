using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Commands.UpdateTicketTypes;
using EventHub.Domain.Entities;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Events;

public sealed class UpdateEventTicketTypesCommandHandlerTests
{
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public async Task Handle_CannotDeleteTicketTypeThatHasSales()
    {
        var organizerId = Guid.NewGuid();
        var ticketType = new TicketType { Name = "VIP", Price = 50m, Quantity = 10, AvailableQuantity = 8 };
        var @event = new Event { OrganizerId = organizerId, TicketTypes = [ticketType] };
        _events.Setup(repository => repository.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _tickets.Setup(repository => repository.GetCountsByTicketTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [ticketType.Id] = 2 });
        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new UpdateEventTicketTypesCommand(@event.Id, []), CancellationToken.None));

        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private UpdateEventTicketTypesCommandHandler CreateHandler() =>
        new(_events.Object, _tickets.Object, _unitOfWork.Object, _currentUser.Object);
}
