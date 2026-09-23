namespace EventHub.Infrastructure.Services;

public sealed class EmailOptions
{
    public const string SectionName = "EmailSettings";

    public required string SmtpServer { get; init; }
    public int SmtpPort { get; init; } = 587;
    public required string SenderName { get; init; }
    public required string SenderEmail { get; init; }
    public required string Password { get; init; }
    public required string VerificationUrl { get; init; }
}
