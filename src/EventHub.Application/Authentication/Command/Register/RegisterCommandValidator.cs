using EventHub.Application.Authentication.Command.Register;
using EventHub.Domain.Constants;
using FluentValidation;

namespace EventHub.Application.Authentication.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(v => v.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("First name cannot be empty.")
            .Length(2, 50).WithMessage("First name must be between 2 and 50 characters.")
            .Matches("^[\\p{L} .'-]+$").WithMessage("First name can only contain letters.");

        RuleFor(v => v.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Last name cannot be empty.")
            .Length(2, 50).WithMessage("Last name must be between 2 and 50 characters.")
            .Matches("^[\\p{L} .'-]+$").WithMessage("Last name can only contain letters.");

        RuleFor(v => v.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .MaximumLength(100).WithMessage("Email can be at most 100 characters long.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(v => v.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password cannot be empty.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password can be at most 128 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(v => v.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Role cannot be empty.")
            .Must(Roles.IsSelfAssignable)
            .WithMessage("You can only select the 'Organizer' or 'Attendee' role.");
    }
}
