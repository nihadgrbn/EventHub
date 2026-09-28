using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Events.Commands.UploadPoster;

public sealed class UploadEventPosterCommandHandler : IRequestHandler<UploadEventPosterCommand, string>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public UploadEventPosterCommandHandler(
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

    public async Task<string> Handle(UploadEventPosterCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (@event is null)
        {
            throw new NotFoundException("Event not found.");
        }

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("A valid user is required to upload an event poster.");

        if (!_currentUserService.IsInRole(Roles.Admin)
            && @event.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You can only upload posters for your own events.");
        }

        if (@event.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw new ConflictException("Cancelled or completed events cannot be edited.");
        }

        // Clean up previous poster if it exists to avoid orphaned files
        if (!string.IsNullOrWhiteSpace(@event.PosterImageUrl))
        {
            await _fileStorageService.DeleteFileAsync(@event.PosterImageUrl, cancellationToken);
        }

        var posterUrl = await _fileStorageService.SavePosterAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            cancellationToken);

        @event.PosterImageUrl = posterUrl;
        _eventRepository.Update(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return posterUrl;
    }
}
