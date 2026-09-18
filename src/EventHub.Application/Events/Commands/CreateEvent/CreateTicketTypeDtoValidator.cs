using FluentValidation;

namespace EventHub.Application.Events.Commands.CreateEvent;

public sealed class CreateTicketTypeDtoValidator : AbstractValidator<CreateTicketTypeDto>
{
    public CreateTicketTypeDtoValidator()
    {
        RuleFor(ticketType => ticketType.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Ticket type name is required.")
            .MaximumLength(100).WithMessage("Ticket type name can be at most 100 characters long.");

        RuleFor(ticketType => ticketType.Price)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Ticket type price must be greater than zero.")
            .LessThanOrEqualTo(9_999_999_999_999_999.99m)
            .WithMessage("Ticket type price can have at most 16 whole digits.")
            .Must(price => decimal.Round(price, 2) == price)
            .WithMessage("Ticket type price can have at most 2 decimal places.");

        RuleFor(ticketType => ticketType.Quantity)
            .GreaterThan(0).WithMessage("Ticket type quantity must be greater than zero.");
    }
}
