using FluentValidation;

namespace EventHub.Application.Events.Queries.GetEvents;

public sealed class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    public GetEventsQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(query => query.SearchTerm)
            .MaximumLength(200)
            .When(query => !string.IsNullOrWhiteSpace(query.SearchTerm))
            .WithMessage("Search term can be at most 200 characters long.");

        RuleFor(query => query.Location)
            .MaximumLength(200)
            .When(query => !string.IsNullOrWhiteSpace(query.Location))
            .WithMessage("Location filter can be at most 200 characters long.");

        RuleFor(query => query)
            .Must(query => !query.DateFrom.HasValue || !query.DateTo.HasValue || query.DateFrom.Value <= query.DateTo.Value)
            .WithMessage("DateFrom must be earlier than or equal to DateTo.");
    }
}
