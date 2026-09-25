using EventHub.Application.Admin.Queries;
using EventHub.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[Authorize(Roles = Roles.Admin)]
[ApiController]
[Route("api/admin")]
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
    public async Task<IActionResult> GetOrganizers([FromQuery] GetAdminUsersQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query with { Role = Roles.Organizer }, cancellationToken));

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] GetAdminEventsQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases([FromQuery] GetAdminPurchasesQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] GetAdminPaymentsQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("reservations")]
    public async Task<IActionResult> GetReservations([FromQuery] GetAdminReservationsQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));
}
