namespace EventHub.Api.Constants;

public static class RateLimitPolicies
{
    public const string AuthStrict = "auth-strict";
    public const string Session = "session";
    public const string PublicRead = "public-read";
    public const string MediaUpload = "media-upload";
    public const string ReservationStrict = "reservation-strict";
    public const string TicketQr = "ticket-qr";
    public const string TicketCheckIn = "ticket-check-in";
    public const string OrganizerRead = "organizer-read";
    public const string AdminRead = "admin-read";
    public const string Webhook = "webhook";
}