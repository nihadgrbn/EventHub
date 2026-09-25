using EventHub.Application.Admin.Models;

namespace EventHub.Application.Common.Interfaces;

public interface IAdminReadRepository
{
    Task<(IReadOnlyList<AdminUserDto> Items, int TotalCount)> GetUsersAsync(
        string? search,
        string? role,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<AdminEventDto> Items, int TotalCount)> GetEventsAsync(
        string? search,
        string? status,
        Guid? organizerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<AdminPurchaseDto> Items, int TotalCount)> GetPurchasesAsync(
        Guid? attendeeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<AdminPaymentDto> Items, int TotalCount)> GetPaymentsAsync(
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<AdminReservationDto> Items, int TotalCount)> GetReservationsAsync(
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
