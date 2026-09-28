using EventHub.Application.Common.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace EventHub.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _uploadDirectory;

    private static readonly Dictionary<string, string> AllowedExtensionMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" }
    };

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;

        var webRoot = !string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? _environment.WebRootPath
            : Path.Combine(_environment.ContentRootPath, "wwwroot");

        _uploadDirectory = Path.Combine(webRoot, "uploads", "posters");

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<string> SavePosterAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ValidationException([new ValidationFailure("File", "File stream is required.")]);
        }

        if (stream.CanSeek && stream.Length == 0)
        {
            throw new ValidationException([new ValidationFailure("File", "File cannot be empty.")]);
        }

        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            throw new ValidationException([new ValidationFailure("File", "File size cannot exceed 5 MB.")]);
        }

        var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensionMimeTypes.ContainsKey(extension))
        {
            throw new ValidationException([new ValidationFailure("File", "Only JPG, PNG, and WebP image formats are allowed.")]);
        }

        var detectedFormat = await DetectImageFormatFromMagicBytesAsync(stream, cancellationToken);
        if (detectedFormat is null)
        {
            throw new ValidationException([new ValidationFailure("File", "File content does not match a valid image signature.")]);
        }

        if (!IsExtensionCompatibleWithFormat(extension, detectedFormat))
        {
            throw new ValidationException([new ValidationFailure("File", "File extension does not match the actual file content type.")]);
        }

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }

        var normalizedExt = NormalizeExtension(detectedFormat);
        var uniqueFileName = $"{Guid.NewGuid():N}{normalizedExt}";
        var destinationPath = Path.Combine(_uploadDirectory, uniqueFileName);

        await using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            await stream.CopyToAsync(destinationStream, cancellationToken);
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/uploads/posters/{uniqueFileName}";
        }

        return $"/uploads/posters/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.CompletedTask;
        }

        try
        {
            string fileName;
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            {
                fileName = Path.GetFileName(uri.LocalPath);
            }
            else
            {
                fileName = Path.GetFileName(fileUrl);
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Task.CompletedTask;
            }

            var fullUploadDirPath = Path.GetFullPath(_uploadDirectory);
            var targetFilePath = Path.GetFullPath(Path.Combine(fullUploadDirPath, fileName));

            if (!targetFilePath.StartsWith(fullUploadDirPath, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }
        }
        catch
        {
            // Ignore failure on cleanup to not disrupt main flow
        }

        return Task.CompletedTask;
    }

    private static async Task<string?> DetectImageFormatFromMagicBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanRead) return null;

        var initialPosition = stream.CanSeek ? stream.Position : 0;
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, 12), cancellationToken);

        if (stream.CanSeek)
        {
            stream.Position = initialPosition;
        }

        if (bytesRead < 3) return null;

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return "jpeg";
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytesRead >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return "png";
        }

        // WebP: RIFF (bytes 0-3: 52 49 46 46) and WEBP (bytes 8-11: 57 45 42 50)
        if (bytesRead >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x41 && header[10] == 0x42 && header[11] == 0x50)
        {
            return "webp";
        }

        return null;
    }

    private static bool IsExtensionCompatibleWithFormat(string extension, string format) =>
        (format == "jpeg" && (extension == ".jpg" || extension == ".jpeg")) ||
        (format == "png" && extension == ".png") ||
        (format == "webp" && extension == ".webp");

    private static string NormalizeExtension(string format) => format switch
    {
        "jpeg" => ".jpg",
        "png" => ".png",
        "webp" => ".webp",
        _ => ".jpg"
    };
}
