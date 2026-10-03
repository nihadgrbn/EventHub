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
    private readonly IEmailService _emailService;

    public UpdateEventStatusCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
    }

    public async Task Handle(UpdateEventStatusCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("A valid user is required to change an event status.");

        var isAdmin = _currentUserService.IsInRole(Roles.Admin);

        if (!isAdmin && @event.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You can only change the status of your own events.");
        }

        if (request.Status == EventStatus.Published && !isAdmin)
        {
            throw new ForbiddenException("Tədbirləri təsdiqləmək və dərc etmək icazəsi yalnız Admin roluna məxsusdur.");
        }

        EnsureValidTransition(@event.Status, request.Status, @event.Date);

        // İmtina və ya geri çəkilmə məntiqi
        if (@event.Status == EventStatus.PendingReview && request.Status == EventStatus.Draft)
        {
            if (isAdmin)
            {
                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    throw new ConflictException("Tədbir imtina edilərkən səbəb (rejection reason) mütləq daxil edilməlidir.");
                }
                @event.RejectionReason = request.RejectionReason;
            }
            else
            {
                @event.RejectionReason = "Təşkilatçı tərəfindən geri çəkildi.";
            }
        }
        else if (request.Status == EventStatus.Published)
        {
            @event.RejectionReason = null;
        }

        @event.Status = request.Status;
        @event.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (@event.Organizer != null && !string.IsNullOrWhiteSpace(@event.Organizer.Email))
        {
            try
            {
                if (request.Status == EventStatus.Published)
                {
                    var subject = "🎉 EventHub: Tədbiriniz təsdiqləndi!";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                            <h2 style='color: #10b981;'>Təbrik edirik, {@event.Organizer.FirstName}!</h2>
                            <p style='font-size: 16px; color: #334155;'>Sizin yaratmış olduğunuz <strong>""{@event.Title}""</strong> adlı tədbir inzibatçılarımız tərəfindən nəzərdən keçirildi və uğurla təsdiqləndi!</p>
                            <p style='font-size: 16px; color: #334155;'>Tədbiriniz artıq rəsmi olaraq platformamızda dərc olunub və bilet satışı aktivdir.</p>
                            <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
                            <p style='font-size: 14px; color: #64748b;'>Tədbir Tarixi: {@event.Date.ToString("dd.MM.yyyy HH:mm")}<br />Məkan: {@event.Location}</p>
                        </div>";

                    await _emailService.SendEmailAsync(@event.Organizer.Email, subject, body, true, null, cancellationToken);
                }
                else if (request.Status == EventStatus.Draft && isAdmin && !string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    var subject = "⚠️ EventHub: Tədbir müraciətiniz rədd edildi";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                            <h2 style='color: #ef4444;'>Salam, {@event.Organizer.FirstName}!</h2>
                            <p style='font-size: 16px; color: #334155;'>Sizin təqdim etdiyiniz <strong>""{@event.Title}""</strong> adlı tədbir inzibatçılarımız tərəfindən nəzərdən keçirildi, lakin bəzi səbəblərdən təsdiqlənmədi.</p>
                            <div style='background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <strong style='color: #991b1b;'>İmtina Səbəbi:</strong>
                                <p style='margin: 5px 0 0 0; color: #7f1d1d;'>{request.RejectionReason}</p>
                            </div>
                            <p style='font-size: 16px; color: #334155;'>Zəhmət olmasa yuxarıdakı qeydləri nəzərə alaraq tədbirinizi yeniləyin və yenidən təsdiqə göndərin.</p>
                        </div>";

                    await _emailService.SendEmailAsync(@event.Organizer.Email, subject, body, true, null, cancellationToken);
                }
            }
            catch
            {
            }
        }
    }

    private static void EnsureValidTransition(EventStatus current, EventStatus target, DateTime eventDate)
    {
        if (current == target)
        {
            throw new ConflictException("The event already has this status.");
        }

        var isValid = (current, target) switch
        {
            (EventStatus.Draft, EventStatus.PendingReview) when eventDate > DateTime.UtcNow => true,

            (EventStatus.PendingReview, EventStatus.Published) when eventDate > DateTime.UtcNow => true,

            (EventStatus.PendingReview, EventStatus.Draft) => true,

            (EventStatus.Draft, EventStatus.Cancelled) => true,

            (EventStatus.Published, EventStatus.Cancelled) when eventDate > DateTime.UtcNow => true,

            _ => false
        };

        if (!isValid)
        {
            throw new ConflictException($"The event cannot transition from {current} to {target}.");
        }
    }
}
