using EventHub.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace EventHub.Application.Events.Commands.UpdateTicketTypes;

public record UpdateEventTicketTypesCommand(Guid Id, List<UpdateTicketTypeDto> TicketTypes) : IRequest, IInvalidatesCache
{
    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => [$"event:{Id}", "events"];
}
