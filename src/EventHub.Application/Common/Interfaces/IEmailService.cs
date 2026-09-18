using System.Threading.Tasks;

namespace EventHub.Application.Common.Interfaces;

public record EmailAttachment(string FileName, byte[] Content, string ContentType);

public interface IEmailService
{
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        IReadOnlyCollection<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default);
}
