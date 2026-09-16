using MediatR;

namespace EventHub.Application.Organizers.Queries.GetStatistics;

public record GetOrganizerStatisticsQuery() : IRequest<OrganizerStatisticsDto>;