using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventHub.Domain.Entities;
using EventHub.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace EventHub.Infrastructure.Tests.Authentication;

public sealed class JwtProviderTests
{
    private readonly JwtOptions _options = new()
    {
        Issuer = "EventHub.TestIssuer",
        Audience = "EventHub.TestAudience",
        SecretKey = "SuperSecretKeyForTestingAtLeast32BytesLong!",
        AccessTokenLifetimeMinutes = 60,
        RefreshTokenLifetimeDays = 7
    };

    [Fact]
    public void Generate_CreatesValidJwtWithExpectedClaims()
    {
        var provider = new JwtProvider(Options.Create(_options));
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jwtuser@example.com",
            Role = "Attendee"
        };

        var tokenString = provider.Generate(user);

        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal(_options.Issuer, token.Issuer);
        Assert.Contains(_options.Audience, token.Audiences);

        var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");

        Assert.NotNull(subClaim);
        Assert.Equal(userId.ToString(), subClaim.Value);

        Assert.NotNull(emailClaim);
        Assert.Equal("jwtuser@example.com", emailClaim.Value);

        Assert.NotNull(roleClaim);
        Assert.Equal("Attendee", roleClaim.Value);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueBase64String()
    {
        var provider = new JwtProvider(Options.Create(_options));

        var token1 = provider.GenerateRefreshToken();
        var token2 = provider.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.False(string.IsNullOrWhiteSpace(token2));
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void HashRefreshToken_ReturnsConsistentSha256Hex()
    {
        var provider = new JwtProvider(Options.Create(_options));
        const string raw = "sample-refresh-token-123";

        var hash1 = provider.HashRefreshToken(raw);
        var hash2 = provider.HashRefreshToken(raw);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // 256 bits = 64 hex characters
    }
}
