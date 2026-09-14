using FluentValidation;

namespace EventHub.Application.Authentication.Queries.Login;

public class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(v => v.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .MaximumLength(100).WithMessage("Email can be at most 100 characters long.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password cannot be empty.")
            .MaximumLength(128).WithMessage("Password can be at most 128 characters long.");
    }
}