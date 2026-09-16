namespace EventHub.Application.Organizers.Queries.GetStatistics;

public record OrganizerStatisticsDto(
    int TotalEvents,
    int TotalTicketsSold,
    decimal TotalRevenue,
    int UpcomingEvents
);