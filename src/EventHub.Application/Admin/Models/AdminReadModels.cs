namespace EventHub.Application.Admin.Models;

public sealed record AdminUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsEmailVerified,
    DateTime CreatedAt);

public sealed record AdminEventDto(
    Guid Id,
    string Title,
    string Status,
    string Category,
    DateTime Date,
    Guid OrganizerId,
    string OrganizerName,
    int TicketTypeCount);

public sealed record AdminPurchaseDto(
    Guid Id,
    Guid AttendeeId,
    string AttendeeName,
    decimal TotalAmount,
    DateTime PurchasedAt,
    int TicketCount);

public sealed record AdminPaymentDto(
    Guid Id,
    Guid AttendeeId,
    string AttendeeName,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime ExpiresAt);

public sealed record AdminReservationDto(
    Guid Id,
    Guid AttendeeId,
    string AttendeeName,
    Guid EventId,
    string EventTitle,
    Guid TicketTypeId,
    int Quantity,
    decimal UnitPrice,
    string Status,
    DateTime ReservedUntil,
    DateTime? ConfirmedAt);
