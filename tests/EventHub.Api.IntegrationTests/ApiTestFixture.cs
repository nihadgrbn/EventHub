using System.Collections.Concurrent;
using EventHub.Application.Common.Interfaces;
using EventHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventHub.Api.IntegrationTests;

public sealed class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("eventhub_test")
        .WithUsername("eventhub_test")
        .WithPassword("eventhub_test_password")
        .Build();

    public FakeEmailService EmailService { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrations", "true");
        builder.UseSetting("Jwt:Issuer", "EventHubApi.Tests");
        builder.UseSetting("Jwt:Audience", "EventHubClient.Tests");
        builder.UseSetting("Jwt:SecretKey", "integration-test-secret-key-with-at-least-32-characters");
        builder.UseSetting("EmailSettings:SmtpServer", "localhost");
        builder.UseSetting("EmailSettings:SmtpPort", "2525");
        builder.UseSetting("EmailSettings:SenderName", "EventHub Tests");
        builder.UseSetting("EmailSettings:SenderEmail", "test@example.com");
        builder.UseSetting("EmailSettings:Password", "not-used-by-fake-email-service");
        builder.UseSetting("EmailSettings:VerificationUrl", "http://localhost:8080/api/Auth/verify-email");

        builder.ConfigureTestServices(services =>
        {
            var emailService = services.Single(descriptor => descriptor.ServiceType == typeof(IEmailService));
            services.Remove(emailService);
            services.AddSingleton<IEmailService>(EmailService);
        });
    }
}

public sealed class FakeEmailService : IEmailService
{
    private readonly ConcurrentQueue<SentEmail> _messages = new();

    public IReadOnlyCollection<SentEmail> Messages => _messages.ToArray();

    public Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        IReadOnlyCollection<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(new SentEmail(toEmail, subject, body));
        return Task.CompletedTask;
    }
}

public sealed record SentEmail(string To, string Subject, string Body);

[CollectionDefinition(Name)]
public sealed class ApiTestCollection : ICollectionFixture<ApiTestFixture>
{
    public const string Name = "API integration tests";
}
