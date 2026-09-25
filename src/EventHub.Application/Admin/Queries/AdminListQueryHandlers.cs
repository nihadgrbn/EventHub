using EventHub.Application.Admin.Models;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Models;
using MediatR;

namespace EventHub.Application.Admin.Queries;

public abstract class AdminListQueryHandlerBase
{
    protected static int NormalizePageNumber(int pageNumber) => Math.Max(pageNumber, 1);
    protected static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, 100);
}

public sealed class GetAdminUsersQueryHandler
    : AdminListQueryHandlerBase, IRequestHandler<GetAdminUsersQuery, PaginatedList<AdminUserDto>>
{
    private readonly IAdminReadRepository _repository;
    public GetAdminUsersQueryHandler(IAdminReadRepository repository) => _repository = repository;

    public async Task<PaginatedList<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var page = NormalizePageNumber(request.PageNumber);
        var size = NormalizePageSize(request.PageSize);
        var (items, count) = await _repository.GetUsersAsync(request.Search, request.Role, page, size, cancellationToken);
        return new PaginatedList<AdminUserDto>(items, count, page, size);
    }
}

public sealed class GetAdminEventsQueryHandler
    : AdminListQueryHandlerBase, IRequestHandler<GetAdminEventsQuery, PaginatedList<AdminEventDto>>
{
    private readonly IAdminReadRepository _repository;
    public GetAdminEventsQueryHandler(IAdminReadRepository repository) => _repository = repository;

    public async Task<PaginatedList<AdminEventDto>> Handle(GetAdminEventsQuery request, CancellationToken cancellationToken)
    {
        var page = NormalizePageNumber(request.PageNumber);
        var size = NormalizePageSize(request.PageSize);
        var (items, count) = await _repository.GetEventsAsync(request.Search, request.Status, request.OrganizerId, page, size, cancellationToken);
        return new PaginatedList<AdminEventDto>(items, count, page, size);
    }
}

public sealed class GetAdminPurchasesQueryHandler
    : AdminListQueryHandlerBase, IRequestHandler<GetAdminPurchasesQuery, PaginatedList<AdminPurchaseDto>>
{
    private readonly IAdminReadRepository _repository;
    public GetAdminPurchasesQueryHandler(IAdminReadRepository repository) => _repository = repository;

    public async Task<PaginatedList<AdminPurchaseDto>> Handle(GetAdminPurchasesQuery request, CancellationToken cancellationToken)
    {
        var page = NormalizePageNumber(request.PageNumber);
        var size = NormalizePageSize(request.PageSize);
        var (items, count) = await _repository.GetPurchasesAsync(request.AttendeeId, page, size, cancellationToken);
        return new PaginatedList<AdminPurchaseDto>(items, count, page, size);
    }
}

public sealed class GetAdminPaymentsQueryHandler
    : AdminListQueryHandlerBase, IRequestHandler<GetAdminPaymentsQuery, PaginatedList<AdminPaymentDto>>
{
    private readonly IAdminReadRepository _repository;
    public GetAdminPaymentsQueryHandler(IAdminReadRepository repository) => _repository = repository;

    public async Task<PaginatedList<AdminPaymentDto>> Handle(GetAdminPaymentsQuery request, CancellationToken cancellationToken)
    {
        var page = NormalizePageNumber(request.PageNumber);
        var size = NormalizePageSize(request.PageSize);
        var (items, count) = await _repository.GetPaymentsAsync(request.Status, page, size, cancellationToken);
        return new PaginatedList<AdminPaymentDto>(items, count, page, size);
    }
}

public sealed class GetAdminReservationsQueryHandler
    : AdminListQueryHandlerBase, IRequestHandler<GetAdminReservationsQuery, PaginatedList<AdminReservationDto>>
{
    private readonly IAdminReadRepository _repository;
    public GetAdminReservationsQueryHandler(IAdminReadRepository repository) => _repository = repository;

    public async Task<PaginatedList<AdminReservationDto>> Handle(GetAdminReservationsQuery request, CancellationToken cancellationToken)
    {
        var page = NormalizePageNumber(request.PageNumber);
        var size = NormalizePageSize(request.PageSize);
        var (items, count) = await _repository.GetReservationsAsync(request.Status, page, size, cancellationToken);
        return new PaginatedList<AdminReservationDto>(items, count, page, size);
    }
}
