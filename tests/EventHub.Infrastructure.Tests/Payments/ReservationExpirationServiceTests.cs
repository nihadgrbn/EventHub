using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EventHub.Infrastructure.Tests.Payments;

public sealed class ReservationExpirationServiceTests
{
    [Fact]
    public async Task ExpireReservations_RestoresInventoryAndMarksReservationExpired()
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Quantity = 2,
            Status = ReservationStatus.Active,
            ReservedUntil = DateTime.UtcNow.AddMinutes(-1)
        };
        var reservations = new Mock<IReservationRepository>();
        var ticketTypes = new Mock<ITicketTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        reservations.Setup(repository => repository.GetExpiredActiveAsync(
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reservation });
        ticketTypes.Setup(repository => repository.TryIncreaseAvailableQuantityAsync(
                reservation.TicketTypeId, reservation.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        unitOfWork.Setup(unit => unit.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, token) => action(token));

        var service = new ReservationExpirationService(
            reservations.Object,
            ticketTypes.Object,
            unitOfWork.Object,
            NullLogger<ReservationExpirationService>.Instance);
        var result = await service.ExpireReservationsAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(1, result);
        reservations.Verify(repository => repository.UpdateStatusAsync(
            reservation.Id,
            ReservationStatus.Expired,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        ticketTypes.Verify(repository => repository.TryIncreaseAvailableQuantityAsync(
            reservation.TicketTypeId,
            2,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExpireReservations_WhenInventoryRestoreFails_DoesNotMarkReservationExpired()
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Quantity = 2,
            Status = ReservationStatus.Active
        };
        var reservations = new Mock<IReservationRepository>();
        var ticketTypes = new Mock<ITicketTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        reservations.Setup(repository => repository.GetExpiredActiveAsync(
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reservation });
        ticketTypes.Setup(repository => repository.TryIncreaseAvailableQuantityAsync(
                reservation.TicketTypeId, reservation.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        unitOfWork.Setup(unit => unit.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, token) => action(token));

        var service = new ReservationExpirationService(
            reservations.Object,
            ticketTypes.Object,
            unitOfWork.Object,
            NullLogger<ReservationExpirationService>.Instance);
        var result = await service.ExpireReservationsAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(0, result);
        reservations.Verify(repository => repository.UpdateStatusAsync(
            It.IsAny<Guid>(),
            It.IsAny<ReservationStatus>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
