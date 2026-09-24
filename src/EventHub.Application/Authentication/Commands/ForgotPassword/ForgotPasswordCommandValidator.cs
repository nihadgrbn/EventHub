using FluentValidation;

namespace EventHub.Application.Authentication.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .Must(value => value == value.Trim())
            .WithMessage("Email cannot start or end with whitespace.")
            .MaximumLength(100).WithMessage("Email can be at most 100 characters long.")
            .EmailAddress().WithMessage("Please enter a valid email address.");
    }
}
