using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Commands.CreateEvent
{
    public record CreateEventCommand(
        string Title,
        string Description,
        DateTime Date,
    string Location) : IRequest<Guid>;
    
}
