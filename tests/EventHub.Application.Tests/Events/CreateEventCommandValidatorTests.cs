using EventHub.Application.Events.Commands.CreateEvent;
using EventHub.Domain.Enums;
using Xunit;

namespace EventHub.Application.Tests.Events;

public sealed class CreateEventCommandValidatorTests
{
    private readonly CreateEventCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(CreateValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidTicketTypes_ReturnsErrorsForEachInvalidField()
    {
        var command = CreateValidCommand() with
        {
            TicketTypes =
            [
                new CreateTicketTypeDto("VIP", 12.345m, 0),
                new CreateTicketTypeDto(" vip ", 0m, 1)
            ]
        };

        var result = _validator.Validate(command);

        Assert.Contains(result.Errors, error => error.PropertyName == "TicketTypes[0].Price");
        Assert.Contains(result.Errors, error => error.PropertyName == "TicketTypes[0].Quantity");
        Assert.Contains(result.Errors, error => error.PropertyName == "TicketTypes[1].Price");
        Assert.Contains(result.Errors, error => error.PropertyName == "TicketTypes");
    }

    [Fact]
    public void Validate_WhitespaceTextAndMissingTickets_ReturnsErrors()
    {
        var command = CreateValidCommand() with
        {
            Title = "   ",
            Location = "\t",
            Description = new string('x', 1001),
            TicketTypes = []
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Title");
        Assert.Contains(result.Errors, error => error.PropertyName == "Location");
        Assert.Contains(result.Errors, error => error.PropertyName == "Description");
        Assert.Contains(result.Errors, error => error.PropertyName == "TicketTypes");
    }

    private static CreateEventCommand CreateValidCommand() => new(
        "Tech Meetup",
        "A community meetup for developers.",
        DateTime.UtcNow.AddDays(7),
        "Baku",
        EventCategory.Other, 
        "1 Neftchilar Avenue, Baku",
        "https://cdn.example.com/poster.png",
        40.4093m,
        49.8671m,
        [new CreateTicketTypeDto("Standard", 20m, 100)]);
}
