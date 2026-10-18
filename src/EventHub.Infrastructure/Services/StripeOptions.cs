namespace EventHub.Infrastructure.Services;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = "http://localhost:3000/payment/success";
    public string CancelUrl { get; init; } = "http://localhost:3000/payment/cancelled";
}
