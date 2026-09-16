using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Authentication;

public sealed class SecureTokenService : ISecureTokenService
{
    private readonly TokenOptions _options;

    public SecureTokenService(IOptions<TokenOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public DateTime GetPasswordResetTokenExpiry()
    {
        return DateTime.UtcNow.AddMinutes(_options.PasswordResetTokenLifetimeMinutes);
    }

    public DateTime GetEmailVerificationTokenExpiry()
    {
        return DateTime.UtcNow.AddHours(_options.EmailVerificationTokenLifetimeHours);
    }
}
