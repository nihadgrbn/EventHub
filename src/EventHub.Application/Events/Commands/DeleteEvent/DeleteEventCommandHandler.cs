using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using MediatR;

namespace EventHub.Application.Events.Commands.DeleteEvent;

public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITicketRepository _ticketRepository;

    public DeleteEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITicketRepository ticketRepository)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken);

        if (@event is null)
        {
            throw new NotFoundException("Event not found.");
        }

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("A valid user is required to delete an event.");

        if (!_currentUserService.IsInRole(Roles.Admin)
            && @event.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You can only delete your own events.");
        }

        if (@event.Status != EventHub.Domain.Enums.EventStatus.Draft)
        {
            throw new ConflictException("Only draft events can be deleted. Cancel a published event instead.");
        }

        var salesByTicketTypeId = await _ticketRepository.GetCountsByTicketTypeIdsAsync(
            @event.TicketTypes.Select(ticketType => ticketType.Id).ToArray(), cancellationToken);

        if (salesByTicketTypeId.Values.Sum() > 0)
        {
            throw new ConflictException("An event with sold tickets cannot be deleted.");
        }

        _eventRepository.Delete(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
