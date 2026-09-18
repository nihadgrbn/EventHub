using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Tickets.Queries.GetTicketQr;

public sealed class GetTicketQrQueryHandler : IRequestHandler<GetTicketQrQuery, TicketQrPayload>
{
    private readonly ITicketRepository _tickets;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public GetTicketQrQueryHandler(ITicketRepository tickets, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _tickets = tickets;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<TicketQrPayload> Handle(GetTicketQrQuery request, CancellationToken cancellationToken)
    {
        var attendeeId = _currentUser.UserId ?? throw new UnauthorizedException("You must be logged in to access a ticket QR code.");
        var ticket = await _tickets.GetTicketByIdAsync(request.TicketId, cancellationToken)
            ?? throw new NotFoundException("Ticket not found.");

        if (ticket.AttendeeId != attendeeId)
            throw new ForbiddenException("You can only access your own ticket QR codes.");
        if (ticket.Event?.Status != EventStatus.Published)
            throw new ConflictException("A QR code is only available for a published event.");
        if (ticket.CheckedInAt.HasValue)
            throw new ConflictException("This ticket has already been checked in.");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        ticket.QrTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new TicketQrPayload(ticket.Id, token);
    }
}
