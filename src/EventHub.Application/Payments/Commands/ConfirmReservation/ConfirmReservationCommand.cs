using MediatR;

namespace EventHub.Application.Payments.Commands.ConfirmReservation;

public sealed record ConfirmReservationCommand(
    Guid ReservationId,
    string? StripeCheckoutSessionId = null,
    string? StripePaymentIntentId = null,
    Guid? StripePaymentId = null,
    long? StripeAmountTotal = null,
    string? StripeCurrency = null,
    string? StripeEventId = null)
    : IRequest<ConfirmReservationResult>;

public sealed record ConfirmReservationResult(
    Guid PaymentId,
    Guid ReservationId,
    Guid PurchaseId,
    IReadOnlyList<Guid> TicketIds);
