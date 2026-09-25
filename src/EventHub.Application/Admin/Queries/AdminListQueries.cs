using EventHub.Application.Admin.Models;
using EventHub.Application.Common.Models;
using MediatR;

namespace EventHub.Application.Admin.Queries;

public sealed record GetAdminUsersQuery(
    string? Search = null,
    string? Role = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminUserDto>>;

public sealed record GetAdminEventsQuery(
    string? Search = null,
    string? Status = null,
    Guid? OrganizerId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminEventDto>>;

public sealed record GetAdminPurchasesQuery(
    Guid? AttendeeId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminPurchaseDto>>;

public sealed record GetAdminPaymentsQuery(
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminPaymentDto>>;

public sealed record GetAdminReservationsQuery(
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminReservationDto>>;
