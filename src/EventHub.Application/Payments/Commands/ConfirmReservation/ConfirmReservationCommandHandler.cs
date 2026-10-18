using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Tickets.Events;
using EventHub.Domain.Entities;
using MediatR;

namespace EventHub.Application.Payments.Commands.ConfirmReservation;

public sealed class ConfirmReservationCommandHandler
    : IRequestHandler<ConfirmReservationCommand, ConfirmReservationResult>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReservationRepository _reservations;
    private readonly IPaymentRepository _payments;
    private readonly IPurchaseRepository _purchases;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeEventRepository _stripeEvents;

    public ConfirmReservationCommandHandler(
        ICurrentUserService currentUser,
        IReservationRepository reservations,
        IPaymentRepository payments,
        IPurchaseRepository purchases,
        ITicketRepository tickets,
        IUserRepository users,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        IStripeEventRepository stripeEvents)
    {
        _currentUser = currentUser;
        _reservations = reservations;
        _payments = payments;
        _purchases = purchases;
        _tickets = tickets;
        _users = users;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _stripeEvents = stripeEvents;
    }

    public async Task<ConfirmReservationResult> Handle(
        ConfirmReservationCommand request,
        CancellationToken cancellationToken)
    {
        Guid attendeeId;
        Reservation? reservation;

        if (request.StripeCheckoutSessionId is not null)
        {
            reservation = await _reservations.GetForWebhookConfirmationAsync(
                request.ReservationId,
                cancellationToken);
            attendeeId = reservation?.AttendeeId
                ?? throw new NotFoundException("Reservation not found.");
        }
        else
        {
            attendeeId = _currentUser.UserId
                ?? throw new UnauthorizedException("You must be logged in to confirm a reservation.");
            reservation = await _reservations.GetForConfirmationAsync(
                request.ReservationId,
                attendeeId,
                cancellationToken)
                ?? throw new NotFoundException("Reservation not found.");
        }

        if (reservation.Payment is null || reservation.Event is null || reservation.TicketType is null)
        {
            throw new ConflictException("Reservation data is incomplete.");
        }

        if (request.StripeCheckoutSessionId is not null
            && !string.Equals(
                reservation.Payment.StripeCheckoutSessionId,
                request.StripeCheckoutSessionId,
                StringComparison.Ordinal))
        {
            throw new ConflictException("The Stripe checkout session does not match the reservation.");
        }

        if (request.StripePaymentId is not null
            && reservation.PaymentId != request.StripePaymentId)
        {
            throw new ConflictException("The Stripe payment does not match the reservation.");
        }

        if (request.StripeAmountTotal is not null
            && request.StripeAmountTotal != ToMinorUnits(reservation.Payment.Amount))
        {
            throw new ConflictException("The Stripe payment amount does not match the reservation.");
        }

        if (request.StripeCurrency is not null
            && !string.Equals(
                reservation.Payment.Currency,
                request.StripeCurrency,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("The Stripe payment currency does not match the reservation.");
        }

        var attendee = await _users.GetByIdAsync(attendeeId, cancellationToken)
            ?? throw new UnauthorizedException("The current user could not be found.");

        var confirmedAt = DateTime.UtcNow;
        if (reservation.Status == Domain.Enums.ReservationStatus.Confirmed
            && reservation.Payment.Status == Domain.Enums.PaymentStatus.Paid)
        {
            throw new ConflictException("The reservation has already been confirmed.");
        }

        var purchase = new Purchase
        {
            AttendeeId = attendeeId,
            TotalAmount = reservation.Quantity * reservation.UnitPrice,
            PurchasedAt = confirmedAt,
            IdempotencyKey = $"reservation:{reservation.Id}"
        };
        var ticketData = Enumerable.Range(0, reservation.Quantity)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)))
            .Select(token => (Token: token, Ticket: new Ticket
            {
                EventId = reservation.EventId,
                TicketTypeId = reservation.TicketTypeId,
                AttendeeId = attendeeId,
                Purchase = purchase,
                PurchaseDate = confirmedAt,
                TicketTypeNameAtPurchase = reservation.TicketType.Name,
                PriceAtPurchase = reservation.UnitPrice,
                QrTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
            }))
            .ToList();
        var tickets = ticketData.Select(item => item.Ticket).ToList();
        purchase.Tickets = tickets;

        var receiptEvent = new PurchaseReceiptEvent(
            purchase.Id,
            $"{attendee.FirstName} {attendee.LastName}",
            attendee.Email,
            reservation.Event.Title,
            reservation.Event.Date,
            ticketData.Select(item => new PurchaseReceiptItem(
                item.Ticket.Id,
                item.Ticket.TicketTypeNameAtPurchase,
                item.Ticket.PriceAtPurchase,
                item.Token)).ToList(),
            purchase.TotalAmount);
        var outboxMessage = new OutboxMessage
        {
            Type = nameof(PurchaseReceiptEvent),
            Payload = JsonSerializer.Serialize(receiptEvent),
            IdempotencyKey = $"purchase-receipt:{purchase.Id}",
            NextAttemptAt = confirmedAt
        };

        await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            if (request.StripeEventId is not null
                && !await _stripeEvents.TryMarkAsProcessedAsync(
                    request.StripeEventId,
                    transactionCancellationToken))
            {
                throw new ConflictException("The Stripe event has already been processed.");
            }

            if (request.StripePaymentIntentId is not null)
            {
                await _payments.SetPaymentIntentIdAsync(
                    reservation.PaymentId,
                    request.StripePaymentIntentId,
                    transactionCancellationToken);
            }

            if (!await _payments.TryMarkAsPaidAsync(
                    reservation.PaymentId,
                    confirmedAt,
                    transactionCancellationToken))
            {
                throw new ConflictException("The payment is no longer pending or has expired.");
            }

            if (!await _reservations.TryConfirmAsync(
                    reservation.Id,
                    confirmedAt,
                    transactionCancellationToken))
            {
                throw new ConflictException("The reservation is no longer active or has expired.");
            }

            await _purchases.AddAsync(purchase, transactionCancellationToken);
            await _tickets.AddRangeAsync(tickets, transactionCancellationToken);
            await _outbox.AddAsync(outboxMessage, transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        return new ConfirmReservationResult(
            reservation.PaymentId,
            reservation.Id,
            purchase.Id,
            tickets.Select(ticket => ticket.Id).ToList());
    }

    private static long ToMinorUnits(decimal amount) =>
        checked((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero));
}
