using EventHub.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Services;

public sealed class PasswordResetLinkBuilder : IPasswordResetLinkBuilder
{
    private readonly EmailOptions _options;

    public PasswordResetLinkBuilder(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public string Build(string email, string token)
    {
        var separator = _options.PasswordResetUrl.Contains('?') ? '&' : '?';
        return $"{_options.PasswordResetUrl}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }
}
