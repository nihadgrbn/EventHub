using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Authentication
{
    public record AuthResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        string Token,
        string RefreshToken
        );
    
}
