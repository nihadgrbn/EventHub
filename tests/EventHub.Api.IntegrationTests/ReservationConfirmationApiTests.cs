using System.Net;
using Xunit;

namespace EventHub.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class ReservationConfirmationApiTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task ManualReservationConfirmationEndpoint_IsUnavailable()
    {
        using var client = fixture.CreateClient();
        var response = await client.PostAsync(
            $"/api/Payments/reservations/{Guid.NewGuid()}/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReservationStatus_RequiresAuthentication()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync($"/api/Payments/reservations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}
