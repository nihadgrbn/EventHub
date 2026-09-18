using FluentValidation;

namespace EventHub.Application.Events.Commands.UpdateTicketTypes;

public sealed class UpdateEventTicketTypesCommandValidator : AbstractValidator<UpdateEventTicketTypesCommand>
{
    public UpdateEventTicketTypesCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.TicketTypes)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("At least one ticket type is required.")
            .NotEmpty().WithMessage("At least one ticket type is required.")
            .Must(ticketTypes => ticketTypes.Count <= 10).WithMessage("An event can have at most 10 ticket types.")
            .Must(ticketTypes => ticketTypes.Where(ticketType => ticketType.Id.HasValue)
                .Select(ticketType => ticketType.Id!.Value)
                .Distinct().Count() == ticketTypes.Count(ticketType => ticketType.Id.HasValue))
            .WithMessage("A ticket type can only be supplied once.")
            .Must(ticketTypes => ticketTypes.Where(ticketType => !string.IsNullOrWhiteSpace(ticketType.Name))
                .Select(ticketType => ticketType.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).Count()
                    == ticketTypes.Count(ticketType => !string.IsNullOrWhiteSpace(ticketType.Name)))
            .WithMessage("Ticket type names must be unique.");

        RuleForEach(command => command.TicketTypes).ChildRules(ticketType =>
        {
            ticketType.RuleFor(type => type.Name)
                .Cascade(CascadeMode.Stop)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Ticket type name is required.")
                .MaximumLength(100).WithMessage("Ticket type name can be at most 100 characters long.");
            ticketType.RuleFor(type => type.Price)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithMessage("Ticket type price must be greater than zero.")
                .LessThanOrEqualTo(9_999_999_999_999_999.99m).WithMessage("Ticket type price can have at most 16 whole digits.")
                .Must(price => decimal.Round(price, 2) == price).WithMessage("Ticket type price can have at most 2 decimal places.");
            ticketType.RuleFor(type => type.Quantity).GreaterThan(0).WithMessage("Ticket type quantity must be greater than zero.");
        });
    }
}
