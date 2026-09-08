using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Events.Commands.UpdateEvent;

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken);

        if (@event is null)
        {
            throw new NotFoundException("Event not found.");
        }

        @event.Title = request.Title;
        @event.Description = request.Description;
        @event.Date = request.Date;
        @event.Location = request.Location;
        @event.OrganizerId = request.OrganizerId;

        _eventRepository.Update(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
