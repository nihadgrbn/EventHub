using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Organizers.Queries.GetStatistics;

public class GetOrganizerStatisticsQueryHandler : IRequestHandler<GetOrganizerStatisticsQuery, OrganizerStatisticsDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrganizerStatisticsQueryHandler(IEventRepository eventRepository, ICurrentUserService currentUserService)
    {
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrganizerStatisticsDto> Handle(GetOrganizerStatisticsQuery request, CancellationToken cancellationToken)
    {
        var organizerId = _currentUserService.UserId;
        if (organizerId is null)
        {
            throw new UnauthorizedException("You are not authorized to access this resource.");
        }

        var scope = _currentUserService.IsInRole(EventHub.Domain.Constants.Roles.Admin)
            ? (Guid?)null
            : organizerId;

        return await _eventRepository.GetOrganizerStatisticsAsync(scope, cancellationToken);
    }
}