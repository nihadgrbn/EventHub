using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Authentication.Command.Register
{
    public record RegisterCommand(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Role) : IRequest<AuthResponse>;
   
}
