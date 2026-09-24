namespace EventHub.Application.Common.Interfaces;

public interface IReservationExpirationService
{
    Task<int> ExpireReservationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken);
}
