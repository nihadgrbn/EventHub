using EventHub.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Services;

public sealed class EmailVerificationLinkBuilder : IEmailVerificationLinkBuilder
{
    private readonly EmailOptions _options;

    public EmailVerificationLinkBuilder(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public string Build(string token)
    {
        var separator = _options.VerificationUrl.Contains('?') ? '&' : '?';
        return $"{_options.VerificationUrl}{separator}token={Uri.EscapeDataString(token)}";
    }
}