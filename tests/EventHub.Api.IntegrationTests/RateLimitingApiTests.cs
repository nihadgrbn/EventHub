using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EventHub.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class RateLimitingApiTests(ApiTestFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task AuthStrictPolicy_ExceedingLimit_Returns429TooManyRequests()
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.42");

        HttpResponseMessage? lastResponse = null;

        // AuthStrict permit limit is 5. Sending 5 requests.
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/Auth/login", new
            {
                email = $"rate-limit-test-{Guid.NewGuid():N}@example.com",
                password = "WrongPassword123!"
            });

            // The first 5 requests should not be rate-limited (they may return 401 Unauthorized)
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        // 6th request within the 1-minute window should be rejected with 429 Too Many Requests
        lastResponse = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "rate-limit-test@example.com",
            password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        Assert.Equal("application/problem+json", lastResponse.Content.Headers.ContentType?.MediaType);

        var content = await lastResponse.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.Equal(429, root.GetProperty("status").GetInt32());
        Assert.Equal("Too Many Requests", root.GetProperty("title").GetString());
        Assert.True(lastResponse.Headers.Contains("Retry-After") || root.TryGetProperty("retryAfterSeconds", out _));
    }
}
