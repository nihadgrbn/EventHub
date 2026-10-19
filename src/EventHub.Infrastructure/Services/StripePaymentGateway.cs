using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Payments.Commands.ConfirmReservation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Security.Cryptography;
using System.Text;

namespace EventHub.Infrastructure.Services;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;
    private readonly ISender _sender;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentStateService _paymentState;
    private readonly IStripeWebhookInboxRepository _webhookInbox;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(
        IOptions<StripeOptions> options,
        ISender sender,
        IPaymentRepository payments,
        IPaymentStateService paymentState,
        IStripeWebhookInboxRepository webhookInbox,
        ILogger<StripePaymentGateway> logger)
    {
        _options = options.Value;
        _sender = sender;
        _payments = payments;
        _paymentState = paymentState;
        _webhookInbox = webhookInbox;
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
                    },
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            ["payment_id"] = request.PaymentId.ToString(),
                            ["reservation_id"] = request.ReservationId.ToString()
                        }
                    }
                },
                new RequestOptions { IdempotencyKey = $"checkout-session:{request.PaymentId:N}" },
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

    public async Task ExpireCheckoutSessionAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken)
    {
        EnsureApiConfigured();

        try
        {
            var service = new SessionService();
            await service.ExpireAsync(
                checkoutSessionId,
                cancellationToken: cancellationToken);
        }
        catch (StripeException exception)
        {
            throw new PaymentProviderException(
                $"Stripe checkout could not be cancelled: {exception.StripeError?.Message ?? "The provider rejected the request."}");
        }
    }

    public async Task HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken)
    {
        EnsureWebhookConfigured();

        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature))
        {
            throw new UnauthorizedException("The Stripe webhook request is invalid.");
        }

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

        if (string.IsNullOrWhiteSpace(stripeEvent.Id))
        {
            throw new UnauthorizedException("The Stripe webhook event ID is missing.");
        }

        var processedAt = DateTime.UtcNow;
        var paymentIdFromEvent = ResolvePaymentId(stripeEvent);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        if (!await _webhookInbox.TryStartProcessingAsync(
                stripeEvent.Id,
                stripeEvent.Type,
                paymentIdFromEvent,
                payloadHash,
                processedAt,
                cancellationToken))
        {
            return;
        }

        var handledSuccessfully = false;
        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.expired" when stripeEvent.Data.Object is Session expiredSession
                    && !string.IsNullOrWhiteSpace(expiredSession.Id):
                    await _paymentState.HandleExpiredCheckoutAsync(
                        expiredSession.Id,
                        stripeEvent.Id,
                        processedAt,
                        cancellationToken);
                    handledSuccessfully = true;
                    return;

                case "payment_intent.payment_failed" when stripeEvent.Data.Object is PaymentIntent failedPaymentIntent
                    && !string.IsNullOrWhiteSpace(failedPaymentIntent.Id):
                    await _paymentState.HandleFailedPaymentIntentAsync(
                        failedPaymentIntent.Id,
                        paymentIdFromEvent,
                        stripeEvent.Id,
                        processedAt,
                        cancellationToken);
                    handledSuccessfully = true;
                    return;

                case "charge.refunded" when stripeEvent.Data.Object is Charge refundedCharge
                    && !string.IsNullOrWhiteSpace(refundedCharge.PaymentIntentId)
                    && refundedCharge.AmountRefunded >= refundedCharge.Amount:
                    _logger.LogCritical(
                        "A full Stripe refund was detected for PaymentIntentId={PaymentIntentId}. Refunds are disabled by policy.",
                        refundedCharge.PaymentIntentId);
                    await _paymentState.HandleRefundedPaymentIntentAsync(
                        refundedCharge.PaymentIntentId,
                        stripeEvent.Id,
                        processedAt,
                        cancellationToken);
                    handledSuccessfully = true;
                    return;

                case "charge.refunded" when stripeEvent.Data.Object is Charge partiallyRefundedCharge:
                    _logger.LogCritical(
                        "A partial Stripe refund was detected for ChargeId={ChargeId}. Partial refunds are prohibited.",
                        partiallyRefundedCharge.Id);
                    handledSuccessfully = true;
                    return;

                case "checkout.session.completed"
                    or "checkout.session.async_payment_succeeded":
                    break;

                default:
                    handledSuccessfully = true;
                    return;
            }

            if (stripeEvent.Data.Object is not Session session
            || string.IsNullOrWhiteSpace(session.Id)
            || string.IsNullOrWhiteSpace(session.PaymentIntentId)
            || session.AmountTotal is null
            || string.IsNullOrWhiteSpace(session.Currency)
            || !string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
            || !session.Metadata.TryGetValue("reservation_id", out var reservationIdValue)
            || !Guid.TryParse(reservationIdValue, out var reservationId)
            || !session.Metadata.TryGetValue("payment_id", out var paymentIdValue)
            || !Guid.TryParse(paymentIdValue, out var confirmedPaymentId))
            {
                handledSuccessfully = true;
                return;
            }

            try
            {
                await _sender.Send(
                new ConfirmReservationCommand(
                    reservationId,
                    session.Id,
                    session.PaymentIntentId,
                    confirmedPaymentId,
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

            handledSuccessfully = true;
        }
        catch (Exception exception)
        {
            await _webhookInbox.MarkFailedAsync(
                stripeEvent.Id,
                exception.Message,
                CancellationToken.None);
            throw;
        }
        finally
        {
            if (handledSuccessfully)
            {
                await _webhookInbox.MarkProcessedAsync(
                    stripeEvent.Id,
                    DateTime.UtcNow,
                    CancellationToken.None);
            }
        }
    }

    private static Guid? ResolvePaymentId(Event stripeEvent) => stripeEvent.Data.Object switch
    {
        Session session when session.Metadata.TryGetValue("payment_id", out var paymentIdValue)
            && Guid.TryParse(paymentIdValue, out var paymentId) => paymentId,
        PaymentIntent paymentIntent when paymentIntent.Metadata.TryGetValue("payment_id", out var paymentIdValue)
            && Guid.TryParse(paymentIdValue, out var paymentId) => paymentId,
        _ => null
    };

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
