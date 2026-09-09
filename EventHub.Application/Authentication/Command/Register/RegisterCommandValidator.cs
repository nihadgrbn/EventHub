using EventHub.Application.Authentication.Command.Register;
using FluentValidation;

namespace EventHub.Application.Authentication.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(v => v.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ad boş ola bilməz.")
            .Length(2, 50).WithMessage("Ad 2-50 simvol aralığında olmalıdır.")
            .Matches("^[\\p{L} .'-]+$").WithMessage("Ad yalnız hərflərdən ibarət olmalıdır.");

        RuleFor(v => v.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Soyad boş ola bilməz.")
            .Length(2, 50).WithMessage("Soyad 2-50 simvol aralığında olmalıdır.")
            .Matches("^[\\p{L} .'-]+$").WithMessage("Soyad yalnız hərflərdən ibarət olmalıdır.");

        RuleFor(v => v.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email boş ola bilməz.")
            .MaximumLength(100).WithMessage("Email ən çox 100 simvol ola bilər.")
            .EmailAddress().WithMessage("Düzgün email formatı daxil edin.");

        RuleFor(v => v.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Parol boş ola bilməz.")
            .MinimumLength(8).WithMessage("Parol ən azı 8 simvol olmalıdır.")
            .MaximumLength(128).WithMessage("Parol ən çox 128 simvol ola bilər.")
            .Matches("[A-Z]").WithMessage("Parolda ən azı bir böyük hərf olmalıdır.")
            .Matches("[a-z]").WithMessage("Parolda ən azı bir kiçik hərf olmalıdır.")
            .Matches("[0-9]").WithMessage("Parolda ən azı bir rəqəm olmalıdır.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Parolda ən azı bir xüsusi simvol olmalıdır.");
    }
}