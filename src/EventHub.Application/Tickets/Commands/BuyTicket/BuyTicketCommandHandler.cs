using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using MediatR;

namespace EventHub.Application.Tickets.Commands.BuyTicket;

public sealed class BuyTicketCommandHandler : IRequestHandler<BuyTicketCommand, IReadOnlyList<Guid>>
{
    private readonly ITicketTypeRepository _ticketTypeRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public BuyTicketCommandHandler(
        ITicketTypeRepository ticketTypeRepository,
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _ticketTypeRepository = ticketTypeRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<Guid>> Handle(
        BuyTicketCommand request,
        CancellationToken cancellationToken)
    {
        var attendeeId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be logged in to buy a ticket.");

        var ticketType = await _ticketTypeRepository.GetByIdAsync(
            request.TicketTypeId,
            cancellationToken);

        if (ticketType is null)
        {
            throw new NotFoundException("Ticket type not found.");
        }

        if (ticketType.EventId != request.EventId)
        {
            throw new ConflictException("The ticket type does not belong to the selected event.");
        }

        var tickets = Enumerable.Range(0, request.Quantity)
            .Select(_ => new Ticket
            {
                EventId = request.EventId,
                TicketTypeId = request.TicketTypeId,
                AttendeeId = attendeeId,
                PurchaseDate = DateTime.UtcNow
            })
            .ToList();

        await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var quantityDecreased = await _ticketTypeRepository
                .TryDecreaseAvailableQuantityAsync(
                    request.TicketTypeId,
                    request.Quantity,
                    transactionCancellationToken);

            if (!quantityDecreased)
            {
                throw new ConflictException("This ticket type is sold out.");
            }

            await _ticketRepository.AddRangeAsync(tickets, transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        return tickets.Select(ticket => ticket.Id).ToList();
    }
}
