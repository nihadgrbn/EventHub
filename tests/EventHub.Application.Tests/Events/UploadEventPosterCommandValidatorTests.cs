using EventHub.Application.Events.Commands.UploadPoster;
using Xunit;

namespace EventHub.Application.Tests.Events;

public sealed class UploadEventPosterCommandValidatorTests
{
    private readonly UploadEventPosterCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadEventPosterCommand(Guid.NewGuid(), stream, "poster.jpg", "image/jpeg", 100);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".sh")]
    [InlineData(".pdf")]
    [InlineData(".txt")]
    [InlineData(".svg")]
    public void Validate_DisallowedExtension_FailsValidation(string extension)
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadEventPosterCommand(Guid.NewGuid(), stream, $"file{extension}", "image/jpeg", 100);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadEventPosterCommand.FileName));
    }

    [Theory]
    [InlineData("application/x-msdownload")]
    [InlineData("text/plain")]
    [InlineData("image/gif")]
    public void Validate_DisallowedContentType_FailsValidation(string contentType)
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadEventPosterCommand(Guid.NewGuid(), stream, "image.png", contentType, 100);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadEventPosterCommand.ContentType));
    }

    [Fact]
    public void Validate_FileSizeExceedsLimit_FailsValidation()
    {
        using var stream = new MemoryStream();
        var command = new UploadEventPosterCommand(Guid.NewGuid(), stream, "poster.webp", "image/webp", 6 * 1024 * 1024);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadEventPosterCommand.Length));
    }

    [Fact]
    public void Validate_EmptyFile_FailsValidation()
    {
        using var stream = new MemoryStream();
        var command = new UploadEventPosterCommand(Guid.NewGuid(), stream, "poster.jpg", "image/jpeg", 0);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadEventPosterCommand.Length));
    }
}
