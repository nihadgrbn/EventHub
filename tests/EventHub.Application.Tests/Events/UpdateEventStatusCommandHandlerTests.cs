using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Commands.UpdateEventStatus;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Events;

public sealed class UpdateEventStatusCommandHandlerTests
{
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public async Task Handle_DraftEventWithFutureDate_PublishesEvent()
    {
        var organizerId = Guid.NewGuid();
        var @event = new Event { OrganizerId = organizerId, Date = DateTime.UtcNow.AddDays(1), Status = EventStatus.Draft };
        _events.Setup(repository => repository.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);

        await CreateHandler().Handle(new UpdateEventStatusCommand(@event.Id, EventStatus.Published), CancellationToken.None);

        Assert.Equal(EventStatus.Published, @event.Status);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DraftEventWithPastDate_CannotPublish()
    {
        var organizerId = Guid.NewGuid();
        var @event = new Event { OrganizerId = organizerId, Date = DateTime.UtcNow.AddMinutes(-1), Status = EventStatus.Draft };
        _events.Setup(repository => repository.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _currentUser.SetupGet(service => service.UserId).Returns(organizerId);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new UpdateEventStatusCommand(@event.Id, EventStatus.Published), CancellationToken.None));
    }

    private UpdateEventStatusCommandHandler CreateHandler() => new(_events.Object, _unitOfWork.Object, _currentUser.Object);
}
