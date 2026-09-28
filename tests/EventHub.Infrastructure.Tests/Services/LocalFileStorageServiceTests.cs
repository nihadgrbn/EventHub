using EventHub.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace EventHub.Infrastructure.Tests.Services;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testTempDir;
    private readonly Mock<IWebHostEnvironment> _environment = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();

    public LocalFileStorageServiceTests()
    {
        _testTempDir = Path.Combine(Path.GetTempPath(), "EventHubTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempDir);
        _environment.SetupGet(e => e.WebRootPath).Returns(_testTempDir);
        _environment.SetupGet(e => e.ContentRootPath).Returns(_testTempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testTempDir))
            {
                Directory.Delete(_testTempDir, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task SavePosterAsync_ValidJpeg_SavesFileAndReturnsUrl()
    {
        var service = new LocalFileStorageService(_environment.Object, _httpContextAccessor.Object);

        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
        using var stream = new MemoryStream(jpegBytes);

        var url = await service.SavePosterAsync(stream, "photo.jpg", "image/jpeg");

        Assert.NotNull(url);
        Assert.StartsWith("/uploads/posters/", url);
        Assert.EndsWith(".jpg", url);

        var fileName = Path.GetFileName(url);
        var savedPath = Path.Combine(_testTempDir, "uploads", "posters", fileName);
        Assert.True(File.Exists(savedPath));
    }

    [Fact]
    public async Task SavePosterAsync_ValidPng_SavesFileAndReturnsUrl()
    {
        var service = new LocalFileStorageService(_environment.Object, _httpContextAccessor.Object);

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        using var stream = new MemoryStream(pngBytes);

        var url = await service.SavePosterAsync(stream, "banner.png", "image/png");

        Assert.NotNull(url);
        Assert.EndsWith(".png", url);

        var fileName = Path.GetFileName(url);
        var savedPath = Path.Combine(_testTempDir, "uploads", "posters", fileName);
        Assert.True(File.Exists(savedPath));
    }

    [Fact]
    public async Task SavePosterAsync_ValidWebp_SavesFileAndReturnsUrl()
    {
        var service = new LocalFileStorageService(_environment.Object, _httpContextAccessor.Object);

        var webpBytes = new byte[]
        {
            0x52, 0x49, 0x46, 0x46, // RIFF
            0x00, 0x00, 0x00, 0x00, // Size
            0x57, 0x41, 0x42, 0x50  // WEBP
        };
        using var stream = new MemoryStream(webpBytes);

        var url = await service.SavePosterAsync(stream, "event.webp", "image/webp");

        Assert.NotNull(url);
        Assert.EndsWith(".webp", url);

        var fileName = Path.GetFileName(url);
        var savedPath = Path.Combine(_testTempDir, "uploads", "posters", fileName);
        Assert.True(File.Exists(savedPath));
    }

    [Fact]
    public async Task SavePosterAsync_FakeExtension_ThrowsValidationException()
    {
        var service = new LocalFileStorageService(_environment.Object, _httpContextAccessor.Object);

        // Plain text content disguised as .jpg
        var fakeBytes = System.Text.Encoding.UTF8.GetBytes("<?php echo 'malicious code'; ?>");
        using var stream = new MemoryStream(fakeBytes);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SavePosterAsync(stream, "exploit.jpg", "image/jpeg"));
    }

    [Fact]
    public async Task SavePosterAsync_MismatchedFormatAndExtension_ThrowsValidationException()
    {
        var service = new LocalFileStorageService(_environment.Object, _httpContextAccessor.Object);

        // Valid PNG bytes but labeled as .jpg
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(pngBytes);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SavePosterAsync(stream, "image.jpg", "image/jpeg"));
    }

    [Fact]
    public async Task DeleteFileAsync_SafeDelete_DeletesOnlyTargetFile()
    {
        var service = new LocalFileStorageService(_environment.Object, _httpContextAccessor.Object);

        var postersDir = Path.Combine(_testTempDir, "uploads", "posters");
        Directory.CreateDirectory(postersDir);

        var testFile = Path.Combine(postersDir, "test-poster.jpg");
        await File.WriteAllTextAsync(testFile, "test image content");

        Assert.True(File.Exists(testFile));

        await service.DeleteFileAsync($"/uploads/posters/test-poster.jpg");

        Assert.False(File.Exists(testFile));
    }

    [Fact]
    public async Task DeleteFileAsync_PathTraversalAttempt_DoesNotDeleteUnauthorizedFile()
    {
        var service = new LocalFileStorageService(_environment.Object, _httpContextAccessor.Object);

        var secretFile = Path.Combine(_testTempDir, "secret.txt");
        await File.WriteAllTextAsync(secretFile, "sensitive data");

        Assert.True(File.Exists(secretFile));

        // Attempt path traversal
        await service.DeleteFileAsync("/uploads/posters/../../secret.txt");

        // The file outside the poster upload directory must remain untouched
        Assert.True(File.Exists(secretFile));
    }
}
