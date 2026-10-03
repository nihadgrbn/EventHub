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
            .Must(status => status is EventStatus.Published or EventStatus.Cancelled or EventStatus.PendingReview or EventStatus.Draft)
            .WithMessage("An event can only be transitioned to Draft, PendingReview, Published, or Cancelled.");
    }
}
