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
    }
}
