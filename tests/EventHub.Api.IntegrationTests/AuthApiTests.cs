using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace EventHub.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class AuthApiTests(ApiTestFixture fixture)
{
    private HttpClient CreateTestClient()
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"198.51.100.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Fact]
    public async Task Register_VerifyEmailAndLogin_CompletesSuccessfully()
    {
        using var client = CreateTestClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Password1!";

        var registerResponse = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            firstName = "Integration",
            lastName = "Tester",
            email,
            password,
            role = "Attendee"
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var verificationEmail = Assert.Single(
            fixture.EmailService.Messages, message => message.To == email);
        var token = ExtractToken(verificationEmail.Body);

        var verifyResponse = await client.PostAsJsonAsync("/api/Auth/verify-email", new
        {
            token
        });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        using var verifyJson = JsonDocument.Parse(await verifyResponse.Content.ReadAsStringAsync());
        Assert.Equal("Email verified successfully.", verifyJson.RootElement.GetProperty("message").GetString());

        var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(
            loginJson.RootElement.GetProperty("data").GetProperty("token").GetString()));
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsUnauthorized()
    {
        using var client = CreateTestClient();
        var response = await client.PostAsJsonAsync("/api/Auth/verify-email", new
        {
            token = "invalid-integration-test-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string ExtractToken(string emailBody)
    {
        var match = Regex.Match(emailBody, "[?&]token=([^\\\"&]+)");
        Assert.True(match.Success, "The verification email did not contain a token link.");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }
}
