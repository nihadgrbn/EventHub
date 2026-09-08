using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Commands.CreateEvent
{
    public class CreateEventCommandValidator: AbstractValidator<CreateEventCommand>
    {
        public CreateEventCommandValidator()
        {
            RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Event title is required.")
            .MaximumLength(200).WithMessage("Event title can be at most 200 characters long.");

            RuleFor(v => v.Location)
                .NotEmpty().WithMessage("Event location is required.");

            RuleFor(v => v.Date)
                .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future.");
            RuleFor(v => v.OrganizerId)
                .NotEmpty().WithMessage("Organizer ID is required.");
        }
    }
}
