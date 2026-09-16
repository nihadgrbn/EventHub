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

        RuleFor(v => v.Date)
            .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future.");


    }
}
