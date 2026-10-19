namespace EventHub.Application.Common.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken);

    Task ExpireCheckoutSessionAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken);

    Task HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken);
}

public sealed record PaymentCheckoutRequest(
    Guid PaymentId,
    Guid ReservationId,
    decimal Amount,
    string Currency,
    DateTime ExpiresAt);

public sealed record PaymentCheckoutSession(
    string SessionId,
    string CheckoutUrl);
