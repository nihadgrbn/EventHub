using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace EventHub.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class AuthApiTests(ApiTestFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Register_VerifyEmailAndLogin_CompletesSuccessfully()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Password1!";

        var registerResponse = await _client.PostAsJsonAsync("/api/Auth/register", new
        {
            firstName = "Integration",
            lastName = "Tester",
            email,
            password,
            role = "Attendee"
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var verificationEmail = Assert.Single(
            fixture.EmailService.Messages.Where(message => message.To == email));
        var token = ExtractToken(verificationEmail.Body);

        var verifyResponse = await _client.GetAsync(
            $"/api/Auth/verify-email?token={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.Contains("Email verified successfully", await verifyResponse.Content.ReadAsStringAsync());

        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
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
        var response = await _client.GetAsync(
            "/api/Auth/verify-email?token=invalid-integration-test-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string ExtractToken(string emailBody)
    {
        var match = Regex.Match(emailBody, "[?&]token=([^\\\"&]+)");
        Assert.True(match.Success, "The verification email did not contain a token link.");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }
}
