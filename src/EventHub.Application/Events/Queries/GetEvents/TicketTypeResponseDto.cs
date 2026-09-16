namespace EventHub.Application.Events.Queries.GetEvents;

public record TicketTypeResponseDto(
    Guid Id,
    string Name,
    decimal Price,
    int AvailableQuantity
);