using MediatR;

namespace EventHub.Application.Tickets.Queries.GetTicketQr;

public record GetTicketQrQuery(Guid TicketId) : IRequest<TicketQrPayload>;
public record TicketQrPayload(Guid TicketId, string Payload);
