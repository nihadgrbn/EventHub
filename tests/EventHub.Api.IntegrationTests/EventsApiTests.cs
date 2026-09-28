using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EventHub.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class EventsApiTests(ApiTestFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task GetEvents_AnonymousAllowed_ReturnsOkWithEventsList()
    {
        var response = await _client.GetAsync("/api/Events?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("items", out _) || json.RootElement.ValueKind == JsonValueKind.Array || json.RootElement.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task GetEventById_NonExistingId_ReturnsNotFound()
    {
        var randomId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/Events/{randomId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
