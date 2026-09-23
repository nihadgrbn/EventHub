using EventHub.Domain.Enums;
using FluentValidation;

namespace EventHub.Application.Events.Commands.UpdateEventStatus;

public sealed class UpdateEventStatusCommandValidator : AbstractValidator<UpdateEventStatusCommand>
{
    public UpdateEventStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.Status)
            .Must(status => status is EventStatus.Published or EventStatus.Cancelled)
            .WithMessage("An event can only be published or cancelled manually.");
    }
}
