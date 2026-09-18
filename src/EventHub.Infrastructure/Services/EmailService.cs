using EventHub.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace EventHub.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;

    public EmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        IReadOnlyCollection<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress(
            _options.SenderName,
            _options.SenderEmail));

        email.To.Add(MailboxAddress.Parse(toEmail));

        email.Subject = subject;
        var bodyBuilder = new BodyBuilder { HtmlBody = isHtml ? body : null, TextBody = isHtml ? null : body };
        if (attachments is not null)
        {
            foreach (var attachment in attachments)
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }
        }
        email.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            _options.SmtpServer,
            _options.SmtpPort,
            _options.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            _options.SenderEmail,
            _options.Password,
            cancellationToken);

        await smtp.SendAsync(email, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}
