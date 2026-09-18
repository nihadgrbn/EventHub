using FluentValidation;
namespace EventHub.Application.Events.Commands.CreateEvent
{
    public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
    {
        public CreateEventCommandValidator()
        {
            RuleFor(v => v.Title)
                .Cascade(CascadeMode.Stop)
                .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Event title is required.")
                .MaximumLength(200).WithMessage("Event title can be at most 200 characters long.");

            RuleFor(v => v.Description)
                .MaximumLength(1000).WithMessage("Event description can be at most 1000 characters long.");

            RuleFor(v => v.Location)
                .Cascade(CascadeMode.Stop)
                .Must(location => !string.IsNullOrWhiteSpace(location)).WithMessage("Event location is required.")
                .MaximumLength(200).WithMessage("Event location can be at most 200 characters long.");

            RuleFor(v => v.Date)
                .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future.");

            RuleFor(v => v.TicketTypes)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("At least one ticket type is required.")
                .NotEmpty().WithMessage("At least one ticket type is required.")
                .Must(ticketTypes => ticketTypes.Count <= 10)
                .WithMessage("An event can have at most 10 ticket types.");

            RuleForEach(v => v.TicketTypes)
                .SetValidator(new CreateTicketTypeDtoValidator());

            RuleFor(v => v.TicketTypes)
                .Must(ticketTypes => ticketTypes is null || ticketTypes
                    .Where(ticketType => !string.IsNullOrWhiteSpace(ticketType.Name))
                    .Select(ticketType => ticketType.Name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() == ticketTypes.Count(ticketType => !string.IsNullOrWhiteSpace(ticketType.Name)))
                .WithMessage("Ticket type names must be unique.");
        }
    }
}
