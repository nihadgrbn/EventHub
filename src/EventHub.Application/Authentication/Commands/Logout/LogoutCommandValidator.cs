using FluentValidation;

namespace EventHub.Application.Authentication.Commands.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Refresh token cannot be empty.")
            .MaximumLength(512).WithMessage("Refresh token is too long.");
    }
}
