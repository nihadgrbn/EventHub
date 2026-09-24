using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class ReservationConfirmationApiTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task ConfirmReservation_QueuesReceiptAndSendsQrAttachments()
    {
        using var client = fixture.CreateClient();
        var email = $"reservation-{Guid.NewGuid():N}@example.com";
        const string password = "Password1!";

        var registerResponse = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            firstName = "Reservation",
            lastName = "Tester",
            email,
            password,
            role = "Attendee"
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var verificationEmail = Assert.Single(
            fixture.EmailService.Messages.Where(message => message.To == email));
        var token = ExtractToken(verificationEmail.Body);
        var verifyResponse = await client.GetAsync(
            $"/api/Auth/verify-email?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();
        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var accessToken = loginJson.RootElement.GetProperty("data").GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        await SeedEventAsync(fixture, eventId, ticketTypeId, loginJson.RootElement.GetProperty("data").GetProperty("id").GetGuid());

        var reservationResponse = await client.PostAsJsonAsync(
            "/api/Payments/reservations",
            new { eventId, ticketTypeId, quantity = 2 },
            new HttpRequestMessageOptions { Headers = { { "Idempotency-Key", $"integration-{Guid.NewGuid():N}" } } });
        reservationResponse.EnsureSuccessStatusCode();
        using var reservationJson = JsonDocument.Parse(await reservationResponse.Content.ReadAsStringAsync());
        var reservationId = reservationJson.RootElement.GetProperty("reservationId").GetGuid();

        var confirmResponse = await client.PostAsync(
            $"/api/Payments/reservations/{reservationId}/confirm", null);
        confirmResponse.EnsureSuccessStatusCode();
        using var confirmationJson = JsonDocument.Parse(await confirmResponse.Content.ReadAsStringAsync());
        var ticketIds = confirmationJson.RootElement.GetProperty("ticketIds")
            .EnumerateArray()
            .Select(element => element.GetGuid())
            .ToArray();

        var receiptEmail = await WaitForEmailAsync(fixture, email);
        Assert.Contains("purchase receipt", receiptEmail.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reservation Tester", receiptEmail.Body);
        Assert.Equal(ticketIds.Length, receiptEmail.Attachments.Count);
        Assert.All(receiptEmail.Attachments, attachment =>
        {
            Assert.Equal("image/png", attachment.ContentType);
            Assert.EndsWith(".png", attachment.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(attachment.Content);
        });

        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var outbox = await context.OutboxMessages.SingleAsync(message =>
            message.Type == "PurchaseReceiptEvent"
            && message.Payload == string.Empty);
        Assert.Equal("Sent", outbox.Status);
    }

    private static async Task SeedEventAsync(
        ApiTestFixture fixture,
        Guid eventId,
        Guid ticketTypeId,
        Guid organizerId)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Events.Add(new Event
        {
            Id = eventId,
            Title = "Integration Receipt Event",
            Description = "Event used for receipt integration testing.",
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Test venue",
            Category = EventCategory.Conference,
            Address = "Test address",
            Status = EventStatus.Published,
            OrganizerId = organizerId
        });
        context.TicketTypes.Add(new TicketType
        {
            Id = ticketTypeId,
            EventId = eventId,
            Name = "Standard",
            Price = 25m,
            Quantity = 10,
            AvailableQuantity = 10
        });
        await context.SaveChangesAsync();
    }

    private static async Task<SentEmail> WaitForEmailAsync(
        ApiTestFixture fixture,
        string recipient)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!timeout.IsCancellationRequested)
        {
            var email = fixture.EmailService.Messages
                .LastOrDefault(message => message.To == recipient
                    && message.Subject.Contains("purchase receipt", StringComparison.OrdinalIgnoreCase));
            if (email is not null)
            {
                return email;
            }

            await Task.Delay(100, timeout.Token);
        }

        throw new Xunit.Sdk.XunitException("Receipt email was not sent within the test timeout.");
    }

    private static string ExtractToken(string emailBody)
    {
        var match = Regex.Match(emailBody, "[?&]token=([^\\\"&]+)");
        Assert.True(match.Success, "The verification email did not contain a token link.");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }
}

internal static class HttpRequestMessageExtensions
{
    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        HttpRequestMessageOptions options)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(value)
        };
        foreach (var header in options.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client.SendAsync(request);
    }
}

internal sealed class HttpRequestMessageOptions
{
    public Dictionary<string, string> Headers { get; } = new();
}
