namespace EventHub.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Attendee = "Attendee";
    public const string OrganizerOrAdmin = Organizer + "," + Admin;

    public static bool IsSelfAssignable(string? role)
    {
        return string.Equals(role, Organizer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Attendee, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string role)
    {
        return string.Equals(role, Organizer, StringComparison.OrdinalIgnoreCase)
            ? Organizer
            : Attendee;
    }
}
