using MediatR;

namespace EventHub.Application.Payments.Queries.GetReservationStatus;

public sealed record GetReservationStatusQuery(Guid ReservationId)
    : IRequest<ReservationStatusResponse>;

public sealed record ReservationStatusResponse(
    Guid ReservationId,
    Guid PaymentId,
    string PaymentStatus,
    string ReservationStatus,
    decimal Amount,
    string Currency,
    DateTime ExpiresAt,
    string? CheckoutUrl);
