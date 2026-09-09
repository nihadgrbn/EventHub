using FluentValidation;

namespace EventHub.Application.Authentication.Queries.Login;

public class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(v => v.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email boş ola bilməz.")
            .MaximumLength(100).WithMessage("Email ən çox 100 simvol ola bilər.")
            .EmailAddress().WithMessage("Düzgün email formatı daxil edin.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Parol boş ola bilməz.")
            .MaximumLength(128).WithMessage("Parol ən çox 128 simvol ola bilər.");
    }
}