using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Payments.Commands.CreateReservation;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Payments;

public sealed class CreateReservationCommandHandlerTests
{
    private readonly Mock<ITicketTypeRepository> _ticketTypes = new();
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_WhenStockIsAvailable_CreatesPaymentAndReservation()
    {
        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        ConfigureUser(attendeeId);
        ConfigureTicket(eventId, ticketTypeId, 25m);
        _ticketTypes.Setup(repository => repository.TryDecreaseAvailableQuantityAsync(
                ticketTypeId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        ConfigureTransaction();

        var result = await CreateHandler().Handle(
            new CreateReservationCommand(eventId, ticketTypeId, 2, "checkout-1"),
            CancellationToken.None);

        Assert.Equal(50m, result.Amount);
        Assert.Equal("AZN", result.Currency);
        _payments.Verify(repository => repository.AddAsync(
            It.Is<Payment>(payment => payment.Amount == 50m
                && payment.Status == PaymentStatus.Pending
                && payment.IdempotencyKey == "checkout-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        _reservations.Verify(repository => repository.AddAsync(
            It.Is<Reservation>(reservation => reservation.Quantity == 2
                && reservation.Status == ReservationStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStockIsUnavailable_ThrowsConflictAndDoesNotCreateReservation()
    {
        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        ConfigureUser(attendeeId);
        ConfigureTicket(eventId, ticketTypeId, 25m);
        _ticketTypes.Setup(repository => repository.TryDecreaseAvailableQuantityAsync(
                ticketTypeId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ConfigureTransaction();

        await Assert.ThrowsAsync<ConflictException>(() => CreateHandler().Handle(
            new CreateReservationCommand(eventId, ticketTypeId, 2),
            CancellationToken.None));

        _payments.Verify(repository => repository.AddAsync(
            It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _reservations.Verify(repository => repository.AddAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyExists_ReturnsExistingReservation()
    {
        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);
        ConfigureUser(attendeeId);
        _payments.Setup(repository => repository.GetByIdempotencyKeyAsync(
                attendeeId, "checkout-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment
            {
                Id = paymentId,
                AttendeeId = attendeeId,
                Amount = 50m,
                Currency = "AZN",
                ExpiresAt = expiresAt,
                IdempotencyKey = "checkout-1",
                Reservations =
                {
                    new Reservation
                    {
                        Id = reservationId,
                        EventId = eventId,
                        TicketTypeId = ticketTypeId,
                        Quantity = 2
                    }
                }
            });

        var result = await CreateHandler().Handle(
            new CreateReservationCommand(eventId, ticketTypeId, 2, "checkout-1"),
            CancellationToken.None);

        Assert.Equal(paymentId, result.PaymentId);
        Assert.Equal(reservationId, result.ReservationId);
        _ticketTypes.Verify(repository => repository.GetByIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void ConfigureUser(Guid attendeeId)
    {
        _currentUser.SetupGet(service => service.UserId).Returns(attendeeId);
    }

    private void ConfigureTicket(Guid eventId, Guid ticketTypeId, decimal price)
    {
        _ticketTypes.Setup(repository => repository.GetByIdAsync(
                ticketTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketType
            {
                Id = ticketTypeId,
                EventId = eventId,
                Price = price
            });
        _events.Setup(repository => repository.GetByIdAsync(
                eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event
            {
                Id = eventId,
                Status = EventStatus.Published
            });
    }

    private void ConfigureTransaction()
    {
        _unitOfWork.Setup(unit => unit.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, token) => action(token));
    }

    private CreateReservationCommandHandler CreateHandler()
    {
        return new CreateReservationCommandHandler(
            _ticketTypes.Object,
            _events.Object,
            _currentUser.Object,
            _payments.Object,
            _reservations.Object,
            _unitOfWork.Object);
    }
}
