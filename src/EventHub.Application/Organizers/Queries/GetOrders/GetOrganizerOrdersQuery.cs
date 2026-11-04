using EventHub.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace EventHub.Application.Organizers.Queries.GetOrders;

public record GetOrganizerOrdersQuery(
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedList<OrganizerOrderDto>>;

public sealed class GetOrganizerOrdersQueryValidator : AbstractValidator<GetOrganizerOrdersQuery>
{
    public GetOrganizerOrdersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
