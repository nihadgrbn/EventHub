using EventHub.Application.Admin.Models;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class AdminReadRepository : IAdminReadRepository
{
    private readonly ApplicationDbContext _context;

    public AdminReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<AdminUserDto> Items, int TotalCount)> GetUsersAsync(
        string? search, string? role, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking();
        var normalizedSearch = search?.Trim();
        var normalizedRole = role?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(user => user.FirstName.Contains(normalizedSearch)
                || user.LastName.Contains(normalizedSearch)
                || user.Email.Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(normalizedRole))
        {
            query = query.Where(user => user.Role == normalizedRole);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(user => user.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new AdminUserDto(
                user.Id, user.FirstName, user.LastName, user.Email, user.Role,
                user.IsEmailVerified, user.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<AdminEventDto> Items, int TotalCount)> GetEventsAsync(
        string? search, string? status, Guid? organizerId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Events.AsNoTracking();
        var normalizedSearch = search?.Trim();
        var normalizedStatus = status?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(@event => @event.Title.Contains(normalizedSearch)
                || @event.Description.Contains(normalizedSearch)
                || @event.Location.Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            if (!Enum.TryParse<EventStatus>(normalizedStatus, true, out var parsedStatus))
            {
                return (Array.Empty<AdminEventDto>(), 0);
            }

            query = query.Where(@event => @event.Status == parsedStatus);
        }

        if (organizerId.HasValue)
        {
            query = query.Where(@event => @event.OrganizerId == organizerId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(@event => @event.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(@event => new AdminEventDto(
                @event.Id,
                @event.Title,
                @event.Status.ToString(),
                @event.Category.ToString(),
                @event.Date,
                @event.OrganizerId,
                @event.Organizer == null ? "" : @event.Organizer.FirstName + " " + @event.Organizer.LastName,
                @event.TicketTypes.Count))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<AdminPurchaseDto> Items, int TotalCount)> GetPurchasesAsync(
        Guid? attendeeId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Purchases.AsNoTracking();
        if (attendeeId.HasValue)
        {
            query = query.Where(purchase => purchase.AttendeeId == attendeeId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(purchase => purchase.PurchasedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(purchase => new AdminPurchaseDto(
                purchase.Id,
                purchase.AttendeeId,
                purchase.Attendee == null ? "" : purchase.Attendee.FirstName + " " + purchase.Attendee.LastName,
                purchase.TotalAmount,
                purchase.PurchasedAt,
                purchase.Tickets.Count))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<AdminPaymentDto> Items, int TotalCount)> GetPaymentsAsync(
        string? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Payments.AsNoTracking();
        var normalizedStatus = status?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            if (!Enum.TryParse<PaymentStatus>(normalizedStatus, true, out var parsedStatus))
            {
                return (Array.Empty<AdminPaymentDto>(), 0);
            }

            query = query.Where(payment => payment.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(payment => payment.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(payment => new AdminPaymentDto(
                payment.Id,
                payment.AttendeeId,
                payment.Attendee == null ? "" : payment.Attendee.FirstName + " " + payment.Attendee.LastName,
                payment.Amount,
                payment.Currency,
                payment.Status.ToString(),
                payment.CreatedAt,
                payment.PaidAt,
                payment.ExpiresAt))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<AdminReservationDto> Items, int TotalCount)> GetReservationsAsync(
        string? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Reservations.AsNoTracking();
        var normalizedStatus = status?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            if (!Enum.TryParse<ReservationStatus>(normalizedStatus, true, out var parsedStatus))
            {
                return (Array.Empty<AdminReservationDto>(), 0);
            }

            query = query.Where(reservation => reservation.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(reservation => reservation.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(reservation => new AdminReservationDto(
                reservation.Id,
                reservation.AttendeeId,
                reservation.Attendee == null ? "" : reservation.Attendee.FirstName + " " + reservation.Attendee.LastName,
                reservation.EventId,
                reservation.Event == null ? "" : reservation.Event.Title,
                reservation.TicketTypeId,
                reservation.Quantity,
                reservation.UnitPrice,
                reservation.Status.ToString(),
                reservation.ReservedUntil,
                reservation.ConfirmedAt))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
