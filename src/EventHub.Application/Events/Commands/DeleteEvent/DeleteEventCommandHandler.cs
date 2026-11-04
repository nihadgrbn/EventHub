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
    private readonly IFileStorageService _fileStorageService;
    private readonly IReservationRepository? _reservationRepository;

    public DeleteEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITicketRepository ticketRepository,
        IFileStorageService fileStorageService,
        IReservationRepository? reservationRepository = null)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _ticketRepository = ticketRepository;
        _fileStorageService = fileStorageService;
        _reservationRepository = reservationRepository;
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
            throw new ConflictException("Only draft events can be deleted. Published events cannot be deleted or cancelled.");
        }

        var ticketTypeIds = @event.TicketTypes.Select(ticketType => ticketType.Id).ToArray();
        var salesByTicketTypeId = await _ticketRepository.GetCountsByTicketTypeIdsAsync(
            ticketTypeIds, cancellationToken);

        if (salesByTicketTypeId.Values.Sum() > 0)
        {
            throw new ConflictException("An event with sold tickets cannot be deleted.");
        }

        if (_reservationRepository is not null)
        {
            var activeReservations = await _reservationRepository.GetActiveQuantitiesByTicketTypeIdsAsync(
                ticketTypeIds, cancellationToken);
            if (activeReservations.Values.Sum() > 0)
            {
                throw new ConflictException("An event with active reservations cannot be deleted.");
            }
        }

        if (!string.IsNullOrWhiteSpace(@event.PosterImageUrl))
        {
            await _fileStorageService.DeleteFileAsync(@event.PosterImageUrl, cancellationToken);
        }

        _eventRepository.Delete(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
