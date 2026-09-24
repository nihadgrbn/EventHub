using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.Application.Common.Interfaces;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);

    Task<Reservation?> GetForConfirmationAsync(
        Guid reservationId,
        Guid attendeeId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Reservation>> GetExpiredActiveAsync(
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<bool> TryConfirmAsync(
        Guid reservationId,
        DateTime confirmedAt,
        CancellationToken cancellationToken);

    Task UpdateStatusAsync(
        Guid reservationId,
        ReservationStatus status,
        DateTime releasedAt,
        CancellationToken cancellationToken);
}
