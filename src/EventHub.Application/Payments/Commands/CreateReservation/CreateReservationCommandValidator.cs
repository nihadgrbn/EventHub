using FluentValidation;

namespace EventHub.Application.Payments.Commands.CreateReservation;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty().WithMessage("Event ID cannot be empty.");

        RuleFor(command => command.TicketTypeId)
            .NotEmpty().WithMessage("Ticket type ID cannot be empty.");

        RuleFor(command => command.Quantity)
            .InclusiveBetween(1, 10)
            .WithMessage("Quantity must be between 1 and 10.");

        RuleFor(command => command.IdempotencyKey)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Idempotency key is required.")
            .Must(value => value is not null && value == value.Trim())
            .WithMessage("Idempotency key cannot start or end with whitespace.")
            .MaximumLength(100);
    }
}
