using EventHub.Application.Common.Interfaces;

namespace EventHub.Infrastructure.Services;

public sealed class DevelopmentPaymentGateway : IPaymentGateway
{
    public Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentCheckoutSession(
            $"dev_{request.PaymentId:N}",
            $"/api/Payments/reservations/{request.ReservationId}/confirm"));

    public Task HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
