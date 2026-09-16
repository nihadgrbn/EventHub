using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Commands.CreateEvent
{
    public record CreateTicketTypeDto(
    string Name,        
    decimal Price,      
    int Quantity        
);

}
