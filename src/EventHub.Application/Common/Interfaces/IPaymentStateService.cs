namespace EventHub.Application.Common.Interfaces;

public interface IPaymentStateService
{
    Task HandleExpiredCheckoutAsync(
        string checkoutSessionId,
        DateTime occurredAt,
        CancellationToken cancellationToken);

    Task HandleFailedPaymentIntentAsync(
        string paymentIntentId,
        DateTime occurredAt,
        CancellationToken cancellationToken);

    Task HandleRefundedPaymentIntentAsync(
        string paymentIntentId,
        DateTime occurredAt,
        CancellationToken cancellationToken);
}