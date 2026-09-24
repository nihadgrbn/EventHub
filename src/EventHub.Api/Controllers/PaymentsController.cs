using EventHub.Application.Payments.Commands.CreateReservation;
using EventHub.Application.Payments.Commands.ConfirmReservation;
using EventHub.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace EventHub.Api.Controllers;

[Authorize(Roles = Roles.Attendee)]
[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHostEnvironment _environment;

    public PaymentsController(ISender sender, IHostEnvironment environment)
    {
        _sender = sender;
        _environment = environment;
    }

    [HttpPost("reservations")]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationCommand command,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            command with { IdempotencyKey = idempotencyKey },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("reservations/{reservationId:guid}/confirm")]
    public async Task<IActionResult> ConfirmReservation(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _sender.Send(
            new ConfirmReservationCommand(reservationId),
            cancellationToken);

        return Ok(result);
    }
}
