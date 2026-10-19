using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.Infrastructure.Services;

public sealed class PaymentStateService : IPaymentStateService
{
    private readonly IPaymentRepository _payments;
    private readonly IReservationRepository _reservations;
    private readonly ITicketTypeRepository _ticketTypes;
    private readonly ITicketRepository _tickets;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentStateService(
        IPaymentRepository payments,
        IReservationRepository reservations,
        ITicketTypeRepository ticketTypes,
        ITicketRepository tickets,
        IUnitOfWork unitOfWork)
    {
        _payments = payments;
        _reservations = reservations;
        _ticketTypes = ticketTypes;
        _tickets = tickets;
        _unitOfWork = unitOfWork;
    }

    public Task HandleExpiredCheckoutAsync(
        string checkoutSessionId,
        DateTime occurredAt,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            cancellation => _payments.GetByCheckoutSessionIdAsync(
                checkoutSessionId,
                cancellation),
            (payment, cancellation) => _payments.TryMarkAsExpiredAsync(
                payment.Id,
                occurredAt,
                cancellation),
            ReservationStatus.Expired,
            occurredAt,
            cancellationToken);

    public Task HandleRefundedPaymentIntentAsync(
        string paymentIntentId,
        DateTime occurredAt,
        CancellationToken cancellationToken) =>
        RefundAsync(paymentIntentId, occurredAt, cancellationToken);

    public Task HandleFailedPaymentIntentAsync(
        string paymentIntentId,
        DateTime occurredAt,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            cancellation => _payments.GetByPaymentIntentIdAsync(
                paymentIntentId,
                cancellation),
            (payment, cancellation) => _payments.TryMarkAsFailedAsync(
                payment.Id,
                occurredAt,
                cancellation),
            ReservationStatus.Cancelled,
            occurredAt,
            cancellationToken);

    private async Task TransitionAsync(
        Func<CancellationToken, Task<Payment?>> findPaymentAsync,
        Func<Payment, CancellationToken, Task<bool>> markPaymentAsync,
        ReservationStatus reservationStatus,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var payment = await findPaymentAsync(transactionCancellationToken);
            if (payment is null)
            {
                return;
            }

            var reservation = await _reservations.GetActiveByPaymentIdAsync(
                payment.Id,
                transactionCancellationToken);
            if (reservation is null)
            {
                throw new InvalidOperationException(
                    $"No active reservation was found for payment {payment.Id}.");
            }

            if (!await markPaymentAsync(payment, transactionCancellationToken))
            {
                return;
            }

            if (!await _ticketTypes.TryIncreaseAvailableQuantityAsync(
                    reservation.TicketTypeId,
                    reservation.Quantity,
                    transactionCancellationToken))
            {
                throw new InvalidOperationException(
                    $"Unable to restore inventory for reservation {reservation.Id}.");
            }

            await _reservations.UpdateStatusAsync(
                reservation.Id,
                reservationStatus,
                occurredAt,
                transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);
    }

    private async Task RefundAsync(
        string paymentIntentId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var payment = await _payments.GetPaidByPaymentIntentIdAsync(
                paymentIntentId,
                transactionCancellationToken);
            if (payment is null)
            {
                return;
            }

            if (!await _payments.TryMarkAsRefundedByPaymentIntentIdAsync(
                    paymentIntentId,
                    occurredAt,
                    transactionCancellationToken))
            {
                return;
            }

            await _tickets.TryRefundByPaymentIdAsync(
                payment.Id,
                transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);
    }
}