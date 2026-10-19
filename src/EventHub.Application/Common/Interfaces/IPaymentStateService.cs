namespace EventHub.Application.Common.Interfaces;

public interface IPaymentStateService
{
    Task HandleExpiredCheckoutAsync(
        string checkoutSessionId,
        string stripeEventId,
        DateTime occurredAt,
        CancellationToken cancellationToken);

    Task HandleFailedPaymentIntentAsync(
        string paymentIntentId,
        Guid? paymentId,
        string stripeEventId,
        DateTime occurredAt,
        CancellationToken cancellationToken);

    Task HandleRefundedPaymentIntentAsync(
        string paymentIntentId,
        string stripeEventId,
        DateTime occurredAt,
        CancellationToken cancellationToken);
}
