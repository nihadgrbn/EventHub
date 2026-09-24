using FluentValidation;

namespace EventHub.Application.Tickets.Commands.BuyTicket;

public sealed class BuyTicketCommandValidator : AbstractValidator<BuyTicketCommand>
{
    public BuyTicketCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty().WithMessage("Event ID cannot be empty.");

        RuleFor(command => command.TicketTypeId)
            .NotEmpty().WithMessage("Ticket type ID cannot be empty.");

        RuleFor(command => command.Quantity)
            .InclusiveBetween(1, 10)
            .WithMessage("Quantity must be between 1 and 10.");

        RuleFor(command => command.IdempotencyKey)
            .MaximumLength(100)
            .When(command => command.IdempotencyKey is not null);
    }
}
