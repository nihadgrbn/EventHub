using FluentValidation;

namespace EventHub.Application.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(command => command.Token)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Verification token cannot be empty.")
            .Must(value => value == value.Trim())
            .WithMessage("Verification token cannot start or end with whitespace.")
            .MaximumLength(512).WithMessage("Verification token is too long.");
    }
}
