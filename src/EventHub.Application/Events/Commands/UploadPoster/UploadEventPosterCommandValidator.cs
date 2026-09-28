using FluentValidation;

namespace EventHub.Application.Events.Commands.UploadPoster;

public sealed class UploadEventPosterCommandValidator : AbstractValidator<UploadEventPosterCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/pjpeg",
        "image/png",
        "image/x-png",
        "image/webp"
    ];

    public UploadEventPosterCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("Event ID is required.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream is required.");

        RuleFor(x => x.FileName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("File name is required.")
            .Must(HaveAllowedExtension).WithMessage("Only JPG, PNG, and WebP image formats are allowed.");

        RuleFor(x => x.ContentType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(BeAllowedContentType).WithMessage("Only image/jpeg, image/png, and image/webp content types are allowed.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("File cannot be empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("File size cannot exceed 5 MB.");
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) &&
               AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeAllowedContentType(string contentType)
    {
        return AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }
}
