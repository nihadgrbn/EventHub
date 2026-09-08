using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using MediatR;


namespace EventHub.Application.Events.Commands.CreateEvent
{
    public class CreateEventCommandHandler: IRequestHandler<CreateEventCommand,Guid>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IUnitOfWork _unitOfWork;
        public CreateEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
        {
            _eventRepository = eventRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task <Guid> Handle(CreateEventCommand request,CancellationToken cancellationToken)
        {
            var newEvent = new Event
            {
                Title = request.Title,
                Description = request.Description,
                Date = request.Date,
                Location = request.Location,
                OrganizerId = request.OrganizerId
            };
            await _eventRepository.AddAsync(newEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return newEvent.Id;
        }

    }
}
