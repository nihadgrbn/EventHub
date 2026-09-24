using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly ApplicationDbContext _context;

    public ReservationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetForConfirmationAsync(
        Guid reservationId,
        Guid attendeeId,
        CancellationToken cancellationToken)
    {
        return await _context.Reservations
            .Include(reservation => reservation.Payment)
            .Include(reservation => reservation.Event)
            .Include(reservation => reservation.TicketType)
            .SingleOrDefaultAsync(reservation => reservation.Id == reservationId
                && reservation.AttendeeId == attendeeId, cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        await _context.Reservations.AddAsync(reservation, cancellationToken);
    }

    public async Task<bool> TryConfirmAsync(
        Guid reservationId,
        DateTime confirmedAt,
        CancellationToken cancellationToken)
    {
        return await _context.Reservations
            .Where(reservation => reservation.Id == reservationId
                && reservation.Status == ReservationStatus.Active
                && reservation.ReservedUntil > confirmedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(reservation => reservation.Status, ReservationStatus.Confirmed)
                .SetProperty(reservation => reservation.ConfirmedAt, confirmedAt)
                .SetProperty(reservation => reservation.UpdatedAt, confirmedAt), cancellationToken) == 1;
    }

    public async Task<IReadOnlyList<Reservation>> GetExpiredActiveAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return await _context.Reservations
            .Where(reservation => reservation.Status == ReservationStatus.Active
                && reservation.ReservedUntil <= utcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        Guid reservationId,
        ReservationStatus status,
        DateTime releasedAt,
        CancellationToken cancellationToken)
    {
        await _context.Reservations
            .Where(reservation => reservation.Id == reservationId
                && reservation.Status == ReservationStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(reservation => reservation.Status, status)
                .SetProperty(reservation => reservation.ReleasedAt, releasedAt)
                .SetProperty(reservation => reservation.UpdatedAt, releasedAt), cancellationToken);
    }
}
