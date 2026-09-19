using FluentValidation;

namespace EventHub.Application.Events.Commands.UpdateEvent;

public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Event title is required.")
            .MaximumLength(200).WithMessage("Event title can be at most 200 characters long.");

        RuleFor(v => v.Location)
            .NotEmpty().WithMessage("Event location is required.");

        RuleFor(v => v.Description)
            .MaximumLength(1000).WithMessage("Event description can be at most 1000 characters long.");

        RuleFor(v => v.Category)
            .IsInEnum().WithMessage("Invalid event category selected.");
        RuleFor(v => v.Address).Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Event address is required.")
            .MaximumLength(300).WithMessage("Event address can be at most 300 characters long.");
        RuleFor(v => v.PosterImageUrl).MaximumLength(2048).WithMessage("Poster URL can be at most 2048 characters long.")
            .Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo))
            .When(v => !string.IsNullOrWhiteSpace(v.PosterImageUrl)).WithMessage("Poster URL must be an absolute HTTPS URL.");
        RuleFor(v => v.Latitude).InclusiveBetween(-90m, 90m).When(v => v.Latitude.HasValue);
        RuleFor(v => v.Longitude).InclusiveBetween(-180m, 180m).When(v => v.Longitude.HasValue);
        RuleFor(v => v).Must(v => v.Latitude.HasValue == v.Longitude.HasValue)
            .WithMessage("Latitude and longitude must be provided together.");

        RuleFor(v => v.Date)
            .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future.");


    }
}
