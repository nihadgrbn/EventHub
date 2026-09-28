using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Events.Commands.DeletePoster;

public sealed class DeleteEventPosterCommandHandler : IRequestHandler<DeleteEventPosterCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public DeleteEventPosterCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
    }

    public async Task Handle(DeleteEventPosterCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (@event is null)
        {
            throw new NotFoundException("Event not found.");
        }

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("A valid user is required to delete an event poster.");

        if (!_currentUserService.IsInRole(Roles.Admin)
            && @event.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You can only delete posters for your own events.");
        }

        if (@event.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw new ConflictException("Cancelled or completed events cannot be edited.");
        }

        if (!string.IsNullOrWhiteSpace(@event.PosterImageUrl))
        {
            await _fileStorageService.DeleteFileAsync(@event.PosterImageUrl, cancellationToken);
            @event.PosterImageUrl = null;
            _eventRepository.Update(@event);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
