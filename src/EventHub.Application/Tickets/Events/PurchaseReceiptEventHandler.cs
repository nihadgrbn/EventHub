using System.Net;
using EventHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using QRCoder;

namespace EventHub.Application.Tickets.Events;

public sealed class PurchaseReceiptEventHandler : INotificationHandler<PurchaseReceiptEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PurchaseReceiptEventHandler> _logger;

    public PurchaseReceiptEventHandler(
        IEmailService emailService,
        ILogger<PurchaseReceiptEventHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PurchaseReceiptEvent notification, CancellationToken cancellationToken)
    {
        var body = BuildBody(notification);
        var attachments = notification.Items.Select(CreateQrAttachment).ToList();

        try
        {
            await _emailService.SendEmailAsync(
                notification.AttendeeEmail,
                $"EventHub purchase receipt: {notification.EventName}",
                body,
                attachments: attachments,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Receipt email was cancelled for purchase {PurchaseId}.", notification.PurchaseId);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Could not send receipt email for purchase {PurchaseId}.",
                notification.PurchaseId);
            throw;
        }
    }

    private static string BuildBody(PurchaseReceiptEvent notification)
    {
        var attendeeName = WebUtility.HtmlEncode(notification.AttendeeName);
        var eventName = WebUtility.HtmlEncode(notification.EventName);
        var items = string.Join(
            "",
            notification.Items.Select(item => $"<li>{WebUtility.HtmlEncode(item.TicketTypeName)} - {item.Price:0.00} AZN (Ticket: {item.TicketId}; QR attachment: ticket-{item.TicketId}.png)</li>"));

        return $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto">
              <h2>EventHub purchase receipt</h2>
              <p>Hello <strong>{attendeeName}</strong>,</p>
              <p>Your purchase was completed successfully.</p>
              <h3>{eventName}</h3>
              <p>Date: {notification.EventDate:dd MMM yyyy, HH:mm}</p>
              <ul>{items}</ul>
              <p><strong>Total: {notification.TotalAmount:0.00} AZN</strong></p>
              <p>Purchase ID: {notification.PurchaseId}</p>
            </div>
            """;
    }

    private static EmailAttachment CreateQrAttachment(PurchaseReceiptItem item)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(item.QrToken, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(12);
        return new EmailAttachment($"ticket-{item.TicketId}.png", png, "image/png");
    }
}
