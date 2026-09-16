using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using MediatR;

namespace EventHub.Application.Events.Commands.UpdateEvent;

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken);

        if (@event is null)
        {
            throw new NotFoundException("Event not found.");
        }

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("A valid user is required to update an event.");

        if (!_currentUserService.IsInRole(Roles.Admin)
            && @event.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You can only update your own events.");
        }

        @event.Title = request.Title;
        @event.Description = request.Description;
        @event.Date = request.Date;
        @event.Location = request.Location;

        _eventRepository.Update(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
