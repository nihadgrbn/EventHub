using EventHub.Application.Tickets.Commands.BuyTicket;
using EventHub.Application.Tickets.Commands.CheckInTicket;
using EventHub.Application.Tickets.Queries.GetTicketQr;
using EventHub.Application.Tickets.Queries.GetMyTickets;
using EventHub.Domain.Constants;
using EventHub.Api.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QRCoder;

namespace EventHub.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("my-tickets")]
    [Authorize(Roles = Roles.Attendee)]
    public async Task<IActionResult> GetMyTickets(CancellationToken cancellationToken)
    {
        var query = new GetMyTicketsQuery();
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{ticketId}/qr")]
    [Authorize(Roles = Roles.Attendee)]
    [EnableRateLimiting(RateLimitPolicies.TicketQr)]
    public async Task<IActionResult> GetQrCode(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTicketQrQuery(ticketId), cancellationToken);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(result.Payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(12);
        Response.Headers["X-Qr-Token"] = result.Payload;
        return File(png, "image/png", $"ticket-{result.TicketId}.png");
    }

    [HttpGet("{ticketId}/qr-pass")]
    [Authorize(Roles = Roles.Attendee)]
    [EnableRateLimiting(RateLimitPolicies.TicketQr)]
    public async Task<IActionResult> GetQrPass(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTicketQrQuery(ticketId), cancellationToken);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(result.Payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(12);
        var base64Image = $"data:image/png;base64,{Convert.ToBase64String(png)}";

        return Ok(new
        {
            ticketId = result.TicketId,
            qrToken = result.Payload,
            qrImageUrl = base64Image
        });
    }

    [HttpPost("check-in")]
    [Authorize(Roles = Roles.OrganizerOrAdmin)]
    [EnableRateLimiting(RateLimitPolicies.TicketCheckIn)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInTicketCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
