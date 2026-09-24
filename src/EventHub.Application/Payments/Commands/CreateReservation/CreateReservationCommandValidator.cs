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
            .Must(value => value is null || value == value.Trim())
            .WithMessage("Idempotency key cannot start or end with whitespace.")
            .MaximumLength(100)
            .When(command => command.IdempotencyKey is not null);
    }
}
