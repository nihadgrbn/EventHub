using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Payments.Commands.ConfirmReservation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace EventHub.Infrastructure.Services;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;
    private readonly ISender _sender;
    private readonly IPaymentRepository _payments;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(
        IOptions<StripeOptions> options,
        ISender sender,
        IPaymentRepository payments,
        ILogger<StripePaymentGateway> logger)
    {
        _options = options.Value;
        _sender = sender;
        _payments = payments;
        _logger = logger;
    }

    public async Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        EnsureApiConfigured();

        try
        {
            var service = new SessionService();
            var session = await service.CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "payment",
                    SuccessUrl = _options.SuccessUrl,
                    CancelUrl = _options.CancelUrl,
                    ClientReferenceId = request.ReservationId.ToString(),
                    ExpiresAt = request.ExpiresAt,
                    PaymentMethodTypes = ["card"],
                    LineItems =
                    [
                        new SessionLineItemOptions
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = request.Currency.ToLowerInvariant(),
                                UnitAmount = ToMinorUnits(request.Amount),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "EventHub ticket reservation"
                                }
                            }
                        }
                    ],
                    Metadata = new Dictionary<string, string>
                    {
                        ["payment_id"] = request.PaymentId.ToString(),
                        ["reservation_id"] = request.ReservationId.ToString()
                    }
                },
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
            {
                throw new PaymentProviderException("Stripe did not return a valid checkout session.");
            }

            return new PaymentCheckoutSession(session.Id, session.Url);
        }
        catch (StripeException exception)
        {
            throw new PaymentProviderException(
                $"Stripe checkout could not be created: {exception.StripeError?.Message ?? "The provider rejected the request."}");
        }
    }

    public async Task HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken)
    {
        EnsureWebhookConfigured();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _options.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException exception)
        {
            _logger.LogWarning(
                exception,
                "Stripe webhook signature validation failed. PayloadLength={PayloadLength}, HasSignature={HasSignature}.",
                payload.Length,
                !string.IsNullOrWhiteSpace(signature));

            throw new UnauthorizedException("The Stripe webhook signature is invalid.");
        }

        var processedAt = DateTime.UtcNow;
        switch (stripeEvent.Type)
        {
            case "checkout.session.expired" when stripeEvent.Data.Object is Session expiredSession
                && !string.IsNullOrWhiteSpace(expiredSession.Id):
                await _payments.TryMarkAsExpiredByCheckoutSessionIdAsync(
                    expiredSession.Id,
                    processedAt,
                    cancellationToken);
                return;

            case "payment_intent.payment_failed" when stripeEvent.Data.Object is PaymentIntent failedPaymentIntent
                && !string.IsNullOrWhiteSpace(failedPaymentIntent.Id):
                await _payments.TryMarkAsFailedByPaymentIntentIdAsync(
                    failedPaymentIntent.Id,
                    processedAt,
                    cancellationToken);
                return;

            case "charge.refunded" when stripeEvent.Data.Object is Charge refundedCharge
                && !string.IsNullOrWhiteSpace(refundedCharge.PaymentIntentId):
                await _payments.TryMarkAsRefundedByPaymentIntentIdAsync(
                    refundedCharge.PaymentIntentId,
                    processedAt,
                    cancellationToken);
                return;

            case "checkout.session.completed"
                or "checkout.session.async_payment_succeeded":
                break;

            default:
                return;
        }

        if (stripeEvent.Data.Object is not Session session
            || !string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
            || !session.Metadata.TryGetValue("reservation_id", out var reservationIdValue)
            || !Guid.TryParse(reservationIdValue, out var reservationId)
            || !session.Metadata.TryGetValue("payment_id", out var paymentIdValue)
            || !Guid.TryParse(paymentIdValue, out var paymentId))
        {
            return;
        }

        try
        {
            await _sender.Send(
                new ConfirmReservationCommand(
                    reservationId,
                    session.Id,
                    session.PaymentIntentId,
                    paymentId,
                    session.AmountTotal,
                    session.Currency,
                    stripeEvent.Id),
                cancellationToken);
        }
        catch (ConflictException exception)
            when (exception.Message is "The reservation has already been confirmed."
                or "The Stripe event has already been processed.")
        {
            // Stripe retries webhook delivery; an already completed reservation is idempotent.
        }
    }

    private void EnsureApiConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new PaymentProviderException(
                "Stripe is not configured. Set Stripe:SecretKey.");
        }

        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    private void EnsureWebhookConfigured()
    {
        EnsureApiConfigured();

        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            throw new PaymentProviderException(
                "Stripe webhook is not configured. Set Stripe:WebhookSecret.");
        }
    }

    private static long ToMinorUnits(decimal amount) =>
        checked((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero));
}
