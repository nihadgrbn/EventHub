using FluentValidation;

namespace EventHub.Application.Payments.Commands.CreateReservation;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("EventId is required.");

        RuleFor(x => x.TicketTypeId)
            .NotEmpty()
            .WithMessage("TicketTypeId is required.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 20)
            .WithMessage("Quantity must be between 1 and 20 tickets per reservation.");

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey))
            .WithMessage("IdempotencyKey cannot exceed 100 characters.");
    }
}
