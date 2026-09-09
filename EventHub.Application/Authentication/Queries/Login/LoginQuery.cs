using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Authentication.Queries.Login
{
    public record LoginQuery(
    string Email,
    string Password
) : IRequest<AuthResponse>;
}
