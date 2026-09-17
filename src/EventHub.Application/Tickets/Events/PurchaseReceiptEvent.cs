using MediatR;

namespace EventHub.Application.Tickets.Events;

public sealed record PurchaseReceiptEvent(
    Guid PurchaseId,
    string AttendeeName,
    string AttendeeEmail,
    string EventName,
    DateTime EventDate,
    IReadOnlyList<PurchaseReceiptItem> Items,
    decimal TotalAmount) : INotification;

public sealed record PurchaseReceiptItem(
    Guid TicketId,
    string TicketTypeName,
    decimal Price);
