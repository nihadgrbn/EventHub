using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Infrastructure.Authentication
{
    public class JwtOptions
    {
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string SecretKey { get; init; } = string.Empty;
        public int AccessTokenLifetimeMinutes { get; init; } = 15;
        public int RefreshTokenLifetimeDays { get; init; } = 7;
    }
}
