using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Tickets.Events;
using EventHub.Domain.Entities;
using MediatR;

namespace EventHub.Application.Tickets.Commands.BuyTicket;

public sealed class BuyTicketCommandHandler : IRequestHandler<BuyTicketCommand, IReadOnlyList<Guid>>
{
    private readonly ITicketTypeRepository _ticketTypeRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;

    public BuyTicketCommandHandler(
        ITicketTypeRepository ticketTypeRepository,
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPurchaseRepository purchaseRepository,
        IOutboxRepository outboxRepository,
        IUserRepository userRepository,
        IEventRepository eventRepository)
    {
        _ticketTypeRepository = ticketTypeRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _purchaseRepository = purchaseRepository;
        _outboxRepository = outboxRepository;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
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

        var attendee = await _userRepository.GetByIdAsync(attendeeId, cancellationToken)
            ?? throw new UnauthorizedException("The current user could not be found.");

        var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        if (eventEntity.Status != EventHub.Domain.Enums.EventStatus.Published)
        {
            throw new ConflictException("Tickets can only be purchased for published events.");
        }

        var purchasedAt = DateTime.UtcNow;

        var purchaseId = Guid.NewGuid();

        var purchase = new Purchase
        {
            Id = purchaseId, 
            AttendeeId = attendeeId,
            PurchasedAt = purchasedAt,
            TotalAmount = ticketType.Price * request.Quantity
        };

        var ticketTokens = Enumerable.Range(0, request.Quantity)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)))
            .ToList();
        var tickets = ticketTokens.Select(token => new Ticket
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            TicketTypeId = request.TicketTypeId,
            AttendeeId = attendeeId,
            PurchaseId = purchase.Id,
            Purchase = purchase,
            PurchaseDate = purchasedAt,
            TicketTypeNameAtPurchase = ticketType.Name,
            PriceAtPurchase = ticketType.Price,
            QrTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
        }).ToList();

        purchase.Tickets = tickets;

        var receiptEvent = new PurchaseReceiptEvent(
            purchase.Id,
            $"{attendee.FirstName} {attendee.LastName}",
            attendee.Email,
            eventEntity.Title,
            eventEntity.Date,
            tickets.Zip(ticketTokens).Select(pair => new PurchaseReceiptItem(
                pair.First.Id,
                pair.First.TicketTypeNameAtPurchase,
                pair.First.PriceAtPurchase,
                pair.Second)).ToList(),
            purchase.TotalAmount);

        var outboxMessage = new OutboxMessage
        {
            Type = nameof(PurchaseReceiptEvent),
            Payload = JsonSerializer.Serialize(receiptEvent),
            IdempotencyKey = $"purchase-receipt:{purchase.Id}",
            NextAttemptAt = purchasedAt
        };

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

            await _purchaseRepository.AddAsync(purchase, transactionCancellationToken);
            await _outboxRepository.AddAsync(outboxMessage, transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        return tickets.Select(ticket => ticket.Id).ToList();
    }
}
