using System.Text.Json.Serialization;
using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Tickets.Commands.BuyTicket;

public sealed record BuyTicketCommand(
    Guid EventId,
    Guid TicketTypeId,
    int Quantity,
    string? IdempotencyKey = null) : IRequest<IReadOnlyList<Guid>>, IInvalidatesCache
{
    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => [$"event:{EventId}", "events"];
}
