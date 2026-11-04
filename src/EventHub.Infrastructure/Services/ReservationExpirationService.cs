using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.Services;

public sealed class ReservationExpirationService : IReservationExpirationService
{
    private readonly IReservationRepository _reservations;
    private readonly IPaymentRepository _payments;
    private readonly ITicketTypeRepository _ticketTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReservationExpirationService> _logger;
    private readonly ICacheService? _cacheService;

    public ReservationExpirationService(
        IReservationRepository reservations,
        IPaymentRepository payments,
        ITicketTypeRepository ticketTypes,
        IUnitOfWork unitOfWork,
        ILogger<ReservationExpirationService> logger,
        ICacheService? cacheService = null)
    {
        _reservations = reservations;
        _payments = payments;
        _ticketTypes = ticketTypes;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<int> ExpireReservationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var expiredReservations = await _reservations.GetExpiredActiveAsync(
            utcNow,
            cancellationToken);
        var expiredCount = 0;
        var affectedEventIds = new HashSet<Guid>();

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

                    if (!await _payments.TryMarkAsExpiredAsync(
                            reservation.PaymentId,
                            utcNow,
                            transactionCancellationToken))
                    {
                        throw new InvalidOperationException(
                            $"Unable to expire payment for reservation {reservation.Id}.");
                    }

                    await _reservations.UpdateStatusAsync(
                        reservation.Id,
                        ReservationStatus.Expired,
                        utcNow,
                        transactionCancellationToken);
                    await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
                }, cancellationToken);

                expiredCount++;
                if (reservation.EventId != Guid.Empty)
                {
                    affectedEventIds.Add(reservation.EventId);
                }
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

        if (expiredCount > 0 && _cacheService is not null)
        {
            foreach (var eventId in affectedEventIds)
            {
                await _cacheService.RemoveByTagAsync($"event:{eventId}", cancellationToken);
            }

            await _cacheService.RemoveByTagAsync("events", cancellationToken);
        }

        return expiredCount;
    }
}
