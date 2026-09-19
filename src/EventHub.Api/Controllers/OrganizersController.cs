using EventHub.Application.Organizers.Queries.GetOrders;
using EventHub.Application.Organizers.Queries.GetStatistics;
using EventHub.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers
{
    [Authorize(Roles = Roles.OrganizerOrAdmin)]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class OrganizersController : ControllerBase
    {
        private readonly ISender _sender;

        public OrganizersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] GetOrganizerOrdersQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
        {
            var query = new GetOrganizerStatisticsQuery();
            var result = await _sender.Send(query, cancellationToken);

            return Ok(result);
        }
    }
}