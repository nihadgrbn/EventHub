using System.Net;
using System.Text.Json;
using Xunit;

namespace EventHub.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class HealthCheckApiTests(ApiTestFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_ReturnsOkWithHealthyStatus()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("checks", out var checks));
        Assert.True(checks.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Healthz_CombinedCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
