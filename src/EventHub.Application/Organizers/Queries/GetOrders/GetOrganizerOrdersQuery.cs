using EventHub.Application.Common.Models;
using MediatR;

namespace EventHub.Application.Organizers.Queries.GetOrders;

public record GetOrganizerOrdersQuery(
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedList<OrganizerOrderDto>>;