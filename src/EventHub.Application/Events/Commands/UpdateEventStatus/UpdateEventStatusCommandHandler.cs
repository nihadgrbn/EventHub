using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Events.Commands.UpdateEventStatus;

public sealed class UpdateEventStatusCommandHandler : IRequestHandler<UpdateEventStatusCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateEventStatusCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateEventStatusCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("A valid user is required to change an event status.");

        if (!_currentUserService.IsInRole(Roles.Admin) && @event.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You can only change the status of your own events.");
        }

        EnsureValidTransition(@event.Status, request.Status, @event.Date);
        @event.Status = request.Status;
        @event.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureValidTransition(EventStatus current, EventStatus target, DateTime eventDate)
    {
        if (current == target)
        {
            throw new ConflictException("The event already has this status.");
        }

        var isValid = (current, target) switch
        {
            (EventStatus.Draft, EventStatus.Published) when eventDate > DateTime.UtcNow => true,
            (EventStatus.Draft, EventStatus.Cancelled) => true,
            (EventStatus.Published, EventStatus.Cancelled) when eventDate > DateTime.UtcNow => true,
            (EventStatus.Published, EventStatus.Completed) when eventDate <= DateTime.UtcNow => true,
            _ => false
        };

        if (!isValid)
        {
            throw new ConflictException($"The event cannot transition from {current} to {target}.");
        }
    }
}
