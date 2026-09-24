using FluentValidation;

namespace EventHub.Application.Authentication.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .Must(value => value == value.Trim())
            .WithMessage("Email cannot start or end with whitespace.")
            .MaximumLength(100).WithMessage("Email can be at most 100 characters long.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(command => command.Token)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Reset token cannot be empty.")
            .Must(value => value == value.Trim())
            .WithMessage("Reset token cannot start or end with whitespace.")
            .MaximumLength(512).WithMessage("Reset token is too long.");

        RuleFor(command => command.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("New password cannot be empty.")
            .Must(value => value == value.Trim())
            .WithMessage("New password cannot start or end with whitespace.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("New password can be at most 128 characters long.")
            .Matches("[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("New password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("New password must contain at least one special character.");
    }
}
