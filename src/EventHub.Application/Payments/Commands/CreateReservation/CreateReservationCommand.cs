using System.Text.Json.Serialization;
using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Payments.Commands.CreateReservation;

public sealed record CreateReservationCommand(
    Guid EventId,
    Guid TicketTypeId,
    int Quantity,
    string? IdempotencyKey = null) : IRequest<CreateReservationResult>, IInvalidatesCache
{
    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => [$"event:{EventId}", "events"];
}

public sealed record CreateReservationResult(
    Guid PaymentId,
    Guid ReservationId,
    decimal Amount,
    string Currency,
    DateTime ExpiresAt,
    string? CheckoutUrl = null);
