using EventHub.Application.Tickets.Commands.BuyTicket;
using EventHub.Application.Tickets.Commands.CheckInTicket;
using EventHub.Application.Tickets.Queries.GetTicketQr;
using EventHub.Application.Tickets.Queries.GetMyTickets;
using EventHub.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        var result = await _sender.Send(query,cancellationToken);

        return Ok(result);
    }

    [HttpPost("buy")]
    [Authorize(Roles = Roles.Attendee)]
    public async Task<IActionResult> BuyTicket(
        [FromBody] BuyTicketCommand command,
        CancellationToken cancellationToken)
    {
        var ticketIds = await _sender.Send(command, cancellationToken);

        return Ok(new
        {
            Message = "Tickets purchased successfully.",
            Quantity = ticketIds.Count,
            TicketIds = ticketIds
        });
    }

    [HttpGet("{ticketId}/qr")]
    [Authorize(Roles = Roles.Attendee)]
    public async Task<IActionResult> GetQrCode(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTicketQrQuery(ticketId), cancellationToken);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(result.Payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(12);
        return File(png, "image/png", $"ticket-{result.TicketId}.png");
    }

    [HttpPost("check-in")]
    [Authorize(Roles = Roles.OrganizerOrAdmin)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInTicketCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }
}
