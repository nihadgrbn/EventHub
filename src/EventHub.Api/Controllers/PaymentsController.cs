using EventHub.Api.Constants;
using EventHub.Application.Payments.Commands.CreateReservation;
using EventHub.Application.Payments.Commands.CancelReservation;
using EventHub.Application.Payments.Queries.GetReservationStatus;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[Authorize(Roles = Roles.Attendee)]
[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IPaymentGateway _paymentGateway;

    public PaymentsController(
        ISender sender,
        IPaymentGateway paymentGateway)
    {
        _sender = sender;
        _paymentGateway = paymentGateway;
    }

    [HttpPost("reservations")]
    [EnableRateLimiting(RateLimitPolicies.ReservationStrict)]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationCommand command,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new
            {
                error = "The Idempotency-Key header is required for reservation requests."
            });
        }

        var result = await _sender.Send(
            command with { IdempotencyKey = idempotencyKey },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("reservations/{reservationId:guid}")]
    public async Task<IActionResult> GetReservationStatus(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetReservationStatusQuery(reservationId),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("reservations/{reservationId:guid}/cancel")]
    [EnableRateLimiting(RateLimitPolicies.ReservationStrict)]
    public async Task<IActionResult> CancelReservation(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new CancelReservationCommand(reservationId), cancellationToken);
        return NoContent();
    }

    [HttpPost("webhook/stripe")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await _paymentGateway.HandleWebhookAsync(payload, signature, cancellationToken);
        return Ok();
    }
}
