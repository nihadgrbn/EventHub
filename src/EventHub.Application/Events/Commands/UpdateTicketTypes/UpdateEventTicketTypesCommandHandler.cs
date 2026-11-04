using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Events.Commands.UpdateTicketTypes;

public sealed class UpdateEventTicketTypesCommandHandler : IRequestHandler<UpdateEventTicketTypesCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IReservationRepository? _reservationRepository;

    public UpdateEventTicketTypesCommandHandler(
        IEventRepository eventRepository,
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IReservationRepository? reservationRepository = null)
    {
        _eventRepository = eventRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _reservationRepository = reservationRepository;
    }

    public async Task Handle(UpdateEventTicketTypesCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("A valid user is required to manage ticket types.");

        if (!_currentUserService.IsInRole(Roles.Admin) && @event.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You can only manage ticket types for your own events.");
        }

        if (@event.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw new ConflictException("Ticket types cannot be changed for cancelled or completed events.");
        }

        var existingById = @event.TicketTypes.ToDictionary(ticketType => ticketType.Id);
        var requestedExistingIds = request.TicketTypes
            .Where(ticketType => ticketType.Id.HasValue)
            .Select(ticketType => ticketType.Id!.Value)
            .ToHashSet();

        if (requestedExistingIds.Any(id => !existingById.ContainsKey(id)))
        {
            throw new ConflictException("One or more ticket types do not belong to this event.");
        }

        var existingIds = existingById.Keys.ToArray();
        var salesByTicketTypeId = await _ticketRepository.GetCountsByTicketTypeIdsAsync(
            existingIds, cancellationToken);
        var activeReservationsByTicketTypeId = _reservationRepository is not null
            ? await _reservationRepository.GetActiveQuantitiesByTicketTypeIdsAsync(existingIds, cancellationToken)
            : new Dictionary<Guid, int>();

        foreach (var existing in existingById.Values.Where(ticketType => !requestedExistingIds.Contains(ticketType.Id)))
        {
            var soldCount = salesByTicketTypeId.GetValueOrDefault(existing.Id);
            var reservedCount = activeReservationsByTicketTypeId.GetValueOrDefault(existing.Id);
            var allocatedFromStock = Math.Max(0, existing.Quantity - existing.AvailableQuantity);
            var committedCount = Math.Max(soldCount + reservedCount, allocatedFromStock);

            if (committedCount > 0)
            {
                throw new ConflictException(
                    $"Ticket type '{existing.Name}' cannot be deleted because tickets have already been sold or reserved.");
            }

            @event.TicketTypes.Remove(existing);
        }

        foreach (var requested in request.TicketTypes)
        {
            if (!requested.Id.HasValue)
            {
                @event.TicketTypes.Add(new TicketType
                {
                    Name = requested.Name.Trim(),
                    Price = requested.Price,
                    Quantity = requested.Quantity,
                    AvailableQuantity = requested.Quantity
                });
                continue;
            }

            var existing = existingById[requested.Id.Value];
            var soldCount = salesByTicketTypeId.GetValueOrDefault(existing.Id);
            var reservedCount = activeReservationsByTicketTypeId.GetValueOrDefault(existing.Id);
            var allocatedFromStock = Math.Max(0, existing.Quantity - existing.AvailableQuantity);
            var committedCount = Math.Max(soldCount + reservedCount, allocatedFromStock);

            if (committedCount > 0 && (!string.Equals(existing.Name, requested.Name.Trim(), StringComparison.Ordinal)
                || existing.Price != requested.Price))
            {
                throw new ConflictException(
                    $"Ticket type '{existing.Name}' has sales or active reservations, so its name and price cannot be changed.");
            }

            if (requested.Quantity < committedCount)
            {
                throw new ConflictException(
                    $"Ticket type '{existing.Name}' cannot have a quantity below its {committedCount} sold or reserved tickets.");
            }

            existing.Name = requested.Name.Trim();
            existing.Price = requested.Price;
            existing.Quantity = requested.Quantity;
            existing.AvailableQuantity = requested.Quantity - committedCount;
        }

        @event.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
