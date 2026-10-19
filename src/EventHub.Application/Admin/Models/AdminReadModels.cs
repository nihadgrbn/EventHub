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
    string Description,
    string Location,
    string Address,
    string? PosterImageUrl,
    string? RejectionReason,
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

public sealed record AdminPaymentDashboardDto(
    int PendingPayments,
    int PaidPayments,
    int FailedPaymentsLast24Hours,
    int ExpiredPaymentsLast24Hours,
    int RefundedPaymentsLast30Days,
    decimal PaidAmountLast30Days,
    int StalePendingPayments,
    int FailedWebhooks,
    int FailedOutboxMessages);

public sealed record AdminWebhookDto(
    Guid Id,
    string EventId,
    string EventType,
    Guid? PaymentId,
    string Status,
    int RetryCount,
    DateTime ReceivedAt,
    DateTime? ProcessedAt,
    string? LastError);

public sealed record AdminOutboxDto(
    Guid Id,
    string Type,
    string Status,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? NextAttemptAt,
    DateTime? ProcessedAt,
    string? LastError);

public sealed record AdminPaymentDetailsDto(
    AdminPaymentDto Payment,
    AdminReservationDto? Reservation,
    AdminPurchaseDto? Purchase,
    IReadOnlyList<AdminWebhookDto> Webhooks);
