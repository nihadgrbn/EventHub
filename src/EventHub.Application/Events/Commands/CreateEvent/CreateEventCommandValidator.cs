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

            RuleFor(v => v.Category)
                .Cascade(CascadeMode.Stop)
                .Must(category => !string.IsNullOrWhiteSpace(category)).WithMessage("Event category is required.")
                .MaximumLength(100).WithMessage("Event category can be at most 100 characters long.");

            RuleFor(v => v.Address)
                .Cascade(CascadeMode.Stop)
                .Must(address => !string.IsNullOrWhiteSpace(address)).WithMessage("Event address is required.")
                .MaximumLength(300).WithMessage("Event address can be at most 300 characters long.");

            RuleFor(v => v.PosterImageUrl)
                .MaximumLength(2048).WithMessage("Poster URL can be at most 2048 characters long.")
                .Must(BeSecureUrl).When(v => !string.IsNullOrWhiteSpace(v.PosterImageUrl))
                .WithMessage("Poster URL must be an absolute HTTPS URL.");

            RuleFor(v => v.Latitude)
                .InclusiveBetween(-90m, 90m).When(v => v.Latitude.HasValue)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(v => v.Longitude)
                .InclusiveBetween(-180m, 180m).When(v => v.Longitude.HasValue)
                .WithMessage("Longitude must be between -180 and 180.");

            RuleFor(v => v)
                .Must(v => v.Latitude.HasValue == v.Longitude.HasValue)
                .WithMessage("Latitude and longitude must be provided together.");

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

        private static bool BeSecureUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && !string.IsNullOrWhiteSpace(uri.Host)
            && string.IsNullOrEmpty(uri.UserInfo);
    }
}
