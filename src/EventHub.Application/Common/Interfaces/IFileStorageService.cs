namespace EventHub.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SavePosterAsync(Stream stream, string originalFileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
