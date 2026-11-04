using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Tickets.Commands.CheckInTicket;

public sealed class CheckInTicketCommandHandler : IRequestHandler<CheckInTicketCommand, CheckInTicketResponseDto>
{
    private readonly ITicketRepository _tickets;
    private readonly ICurrentUserService _currentUser;

    public CheckInTicketCommandHandler(ITicketRepository tickets, ICurrentUserService currentUser)
    {
        _tickets = tickets;
        _currentUser = currentUser;
    }

    public async Task<CheckInTicketResponseDto> Handle(CheckInTicketCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("You must be logged in to check in tickets.");
        var normalizedQrToken = request.QrToken.Trim().ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedQrToken)));
        var ticket = await _tickets.GetByQrTokenHashAsync(hash, cancellationToken);

        if (ticket?.Event is null)
            throw new NotFoundException("Valid ticket not found.");

        if (!_currentUser.IsInRole(Roles.Admin) && ticket.Event.OrganizerId != userId)
            throw new ForbiddenException("You can only check in tickets for your own events.");

        if (ticket.EventId != request.EventId)
            throw new ConflictException($"This ticket belongs to a different event ('{ticket.Event.Title}').");

        if (ticket.Status != TicketStatus.Active)
            throw new ConflictException("This ticket is no longer valid (refunded or cancelled).");

        if (ticket.Event.Status != EventStatus.Published)
            throw new ConflictException("Tickets can only be checked in for published events.");

        if (ticket.CheckedInAt.HasValue)
            throw new ConflictException($"This ticket has already been checked in at {ticket.CheckedInAt.Value:yyyy-MM-dd HH:mm:ss} UTC.");

        var checkedInAt = DateTime.UtcNow;
        if (!await _tickets.TryCheckInAsync(request.EventId, hash, userId, checkedInAt, cancellationToken))
            throw new ConflictException("This QR code has already been used or has expired.");

        var ticketTypeName = !string.IsNullOrWhiteSpace(ticket.TicketTypeNameAtPurchase)
            ? ticket.TicketTypeNameAtPurchase
            : ticket.TicketType?.Name ?? "Standard";

        var priceAtPurchase = ticket.PriceAtPurchase > 0m || ticket.TicketType is null
            ? ticket.PriceAtPurchase
            : ticket.TicketType.Price;

        var attendeeName = ticket.Attendee is not null
            ? $"{ticket.Attendee.FirstName} {ticket.Attendee.LastName}".Trim()
            : "Unknown Attendee";

        var attendeeEmail = ticket.Attendee?.Email ?? string.Empty;

        return new CheckInTicketResponseDto(
            ticket.Id,
            ticket.EventId,
            ticket.Event.Title,
            ticketTypeName,
            priceAtPurchase,
            ticket.AttendeeId,
            attendeeName,
            attendeeEmail,
            checkedInAt);
    }
}
