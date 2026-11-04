using FluentValidation;

namespace EventHub.Application.Authentication.Commands.Refresh;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Refresh token cannot be empty.")
            .MaximumLength(512).WithMessage("Refresh token is too long.");
    }
}
