using FluentValidation;

namespace EventHub.Application.Events.Queries.GetOrganizerEvents;

public sealed class GetOrganizerEventsQueryValidator : AbstractValidator<GetOrganizerEventsQuery>
{
    public GetOrganizerEventsQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}
