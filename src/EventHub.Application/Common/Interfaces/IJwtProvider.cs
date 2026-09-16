using EventHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Common.Interfaces
{
    public interface IJwtProvider
    {
        string Generate(User user);
        string GenerateRefreshToken();
        string HashRefreshToken(string refreshToken);
        DateTime GetRefreshTokenExpiryTime();
    }
}
