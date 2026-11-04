using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Authentication;

public sealed class SecureTokenService : ISecureTokenService
{
    private readonly TokenOptions _options;
    private readonly byte[] _signingKey;

    public SecureTokenService(IOptions<TokenOptions> options, IOptions<JwtOptions> jwtOptions)
    {
        _options = options.Value;
        var secret = !string.IsNullOrWhiteSpace(jwtOptions.Value.SecretKey)
            ? jwtOptions.Value.SecretKey
            : "eventhub-fallback-qr-signing-key-32-bytes";
        _signingKey = SHA256.HashData(Encoding.UTF8.GetBytes($"eventhub:ticket-qr:v1:{secret}"));
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

    public string GenerateTicketQrToken(Guid ticketId)
    {
        var payloadBytes = Encoding.UTF8.GetBytes($"ticket:{ticketId:N}");
        var mac = HMACSHA256.HashData(_signingKey, payloadBytes);
        return Convert.ToHexString(mac);
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
