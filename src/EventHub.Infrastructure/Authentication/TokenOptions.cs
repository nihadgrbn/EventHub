namespace EventHub.Infrastructure.Authentication;

public sealed class TokenOptions
{
    public int PasswordResetTokenLifetimeMinutes { get; init; } = 30;
    public int EmailVerificationTokenLifetimeHours { get; init; } = 24;
}
