using FluentValidation;

namespace EventHub.Application.Tickets.Commands.CheckInTicket;

public sealed class CheckInTicketCommandValidator : AbstractValidator<CheckInTicketCommand>
{
    public CheckInTicketCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.QrToken)
            .NotEmpty()
            .Matches(@"^\s*[A-Fa-f0-9]{64}\s*$")
            .WithMessage("QR token is invalid.");
    }
}
