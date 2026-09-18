using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Tickets.Commands.CheckInTicket;

public sealed class CheckInTicketCommandHandler : IRequestHandler<CheckInTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly ICurrentUserService _currentUser;

    public CheckInTicketCommandHandler(ITicketRepository tickets, ICurrentUserService currentUser)
    {
        _tickets = tickets;
        _currentUser = currentUser;
    }

    public async Task Handle(CheckInTicketCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("You must be logged in to check in tickets.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.QrToken)));
        var ticket = await _tickets.GetByQrTokenHashAsync(hash, cancellationToken);

        if (ticket?.Event is null || ticket.EventId != request.EventId)
            throw new NotFoundException("Valid ticket not found.");
        if (!_currentUser.IsInRole(Roles.Admin) && ticket.Event.OrganizerId != userId)
            throw new ForbiddenException("You can only check in tickets for your own events.");
        if (ticket.Event.Status != EventStatus.Published)
            throw new ConflictException("Tickets can only be checked in for published events.");

        if (!await _tickets.TryCheckInAsync(request.EventId, hash, userId, DateTime.UtcNow, cancellationToken))
            throw new ConflictException("This QR code has already been used or has expired.");
    }
}
