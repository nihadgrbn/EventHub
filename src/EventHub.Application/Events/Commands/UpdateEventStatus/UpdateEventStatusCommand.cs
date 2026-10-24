using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace EventHub.Application.Events.Commands.UpdateEventStatus;

public record UpdateEventStatusCommand(Guid Id, EventStatus Status, string? RejectionReason = null) : IRequest, IInvalidatesCache
{
    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => [$"event:{Id}", "events"];
}
