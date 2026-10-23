using EventHub.Application.Admin.Queries;
using EventHub.Domain.Constants;
using EventHub.Api.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[Authorize(Roles = Roles.Admin)]
[ApiController]
[Route("api/admin")]
[EnableRateLimiting(RateLimitPolicies.AdminRead)]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] GetAdminUsersQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("organizers")]
    public async Task<IActionResult> GetOrganizers([FromQuery] GetAdminUsersQuery query,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(query with { Role = Roles.Organizer }, cancellationToken));

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] GetAdminEventsQuery query,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases([FromQuery] GetAdminPurchasesQuery query,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] GetAdminPaymentsQuery query,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("reservations")]
    public async Task<IActionResult> GetReservations([FromQuery] GetAdminReservationsQuery query,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("payment-dashboard")]
    public async Task<IActionResult> GetPaymentDashboard(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetAdminPaymentDashboardQuery(), cancellationToken));

    [HttpGet("payments/{paymentId:guid}")]
    public async Task<IActionResult> GetPaymentDetails(Guid paymentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAdminPaymentDetailsQuery(paymentId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("payment-webhooks")]
    public async Task<IActionResult> GetPaymentWebhooks([FromQuery] GetAdminWebhooksQuery query,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutboxMessages([FromQuery] GetAdminOutboxMessagesQuery query,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));
}