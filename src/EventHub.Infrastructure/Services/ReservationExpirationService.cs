using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.Services;

public sealed class ReservationExpirationService : IReservationExpirationService
{
    private readonly IReservationRepository _reservations;
    private readonly ITicketTypeRepository _ticketTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReservationExpirationService> _logger;

    public ReservationExpirationService(
        IReservationRepository reservations,
        ITicketTypeRepository ticketTypes,
        IUnitOfWork unitOfWork,
        ILogger<ReservationExpirationService> logger)
    {
        _reservations = reservations;
        _ticketTypes = ticketTypes;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ExpireReservationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var expiredReservations = await _reservations.GetExpiredActiveAsync(
            utcNow,
            cancellationToken);
        var expiredCount = 0;

        foreach (var reservation in expiredReservations)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
                {
                    var restored = await _ticketTypes.TryIncreaseAvailableQuantityAsync(
                        reservation.TicketTypeId,
                        reservation.Quantity,
                        transactionCancellationToken);

                    if (!restored)
                    {
                        throw new InvalidOperationException(
                            $"Unable to restore inventory for reservation {reservation.Id}.");
                    }

                    await _reservations.UpdateStatusAsync(
                        reservation.Id,
                        ReservationStatus.Expired,
                        utcNow,
                        transactionCancellationToken);
                    await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
                }, cancellationToken);

                expiredCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to expire reservation {ReservationId}.",
                    reservation.Id);
            }
        }

        return expiredCount;
    }
}
