using EventHub.Api.Constants;
using EventHub.Api.Middleware;
using EventHub.Api.Services;
using EventHub.Application.Common.Behaviors;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Commands.CreateEvent;
using EventHub.Infrastructure.Authentication;
using EventHub.Infrastructure.BackgroundServices;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Repositories;
using EventHub.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Net;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCorsPolicy";

if (builder.Environment.IsProduction()
    && !builder.Configuration.GetValue<bool>("Stripe:Enabled"))
{
    throw new InvalidOperationException(
        "Stripe must be enabled in Production. Refusing to start with the development payment gateway.");
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ??
                     [
                         "http://localhost:3000",
                         "https://localhost:3000",
                         "http://localhost:5173",
                         "https://localhost:5173",
                         "http://127.0.0.1:3000",
                         "http://127.0.0.1:5173"
                     ];
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var useDistributedRateLimiting = !string.IsNullOrWhiteSpace(redisConnectionString);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    var configuredProxies = builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies")
        .Get<string[]>() ?? Array.Empty<string>();

    var trustedProxies = builder.Environment.IsDevelopment()
        ? configuredProxies.Append("127.0.0.1").Append("::1")
        : configuredProxies;

    foreach (var proxy in trustedProxies.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }
});

static string ResolveClientIp(HttpContext httpContext) =>
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous-client";

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please try again later.",
            Type = "https://tools.ietf.org/html/rfc6585#section-4"
        };
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = (int)Math.Max(1, Math.Ceiling(retryAfter.TotalSeconds));
            context.HttpContext.Response.Headers.RetryAfter = seconds.ToString();
            problemDetails.Extensions["retryAfterSeconds"] = seconds;
        }

        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
    };

    options.GlobalLimiter = useDistributedRateLimiting
        ? PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetNoLimiter("distributed"))
        : PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(ResolveClientIp(httpContext), _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 100, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 4, QueueLimit = 0
                }));

    AddFixedPolicy(options, RateLimitPolicies.AuthStrict, 5, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
    AddFixedPolicy(options, RateLimitPolicies.Session, 20, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
    AddSlidingPolicy(options, RateLimitPolicies.PublicRead, 60, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
    AddFixedPolicy(options, RateLimitPolicies.MediaUpload, 5, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
    AddFixedPolicy(options, RateLimitPolicies.ReservationStrict, 10, TimeSpan.FromMinutes(1),
        useDistributedRateLimiting);
    AddFixedPolicy(options, RateLimitPolicies.TicketQr, 20, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
    AddFixedPolicy(options, RateLimitPolicies.TicketCheckIn, 30, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
    AddSlidingPolicy(options, RateLimitPolicies.OrganizerRead, 60, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
    AddSlidingPolicy(options, RateLimitPolicies.AdminRead, 120, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
    AddFixedPolicy(options, RateLimitPolicies.Webhook, 100, TimeSpan.FromMinutes(1), useDistributedRateLimiting);
});

static void AddFixedPolicy(Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options, string policyName,
    int permitLimit, TimeSpan window, bool distributed) =>
    options.AddPolicy(policyName, httpContext =>
        distributed
            ? RateLimitPartition.GetNoLimiter("distributed")
            : RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous-client", _ =>
                    new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));

static void AddSlidingPolicy(Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options, string policyName,
    int permitLimit, TimeSpan window, bool distributed) =>
    options.AddPolicy(policyName, httpContext =>
        distributed
            ? RateLimitPartition.GetNoLimiter("distributed")
            : RateLimitPartition.GetSlidingWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous-client", _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit, Window = window, SegmentsPerWindow = 4, QueueLimit = 0
                    }));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "database",
        tags: ["ready"])
    .AddCheck<RedisHealthCheck>(
        name: "redis",
        tags: ["ready"]);

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<TokenOptions>(
    builder.Configuration.GetSection("TokenOptions"));
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .Validate(options =>
            Uri.TryCreate(options.VerificationUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            Uri.TryCreate(options.PasswordResetUrl, UriKind.Absolute, out var resetUri) &&
            (resetUri.Scheme == Uri.UriSchemeHttp || resetUri.Scheme == Uri.UriSchemeHttps),
        "EmailSettings:VerificationUrl and PasswordResetUrl must be absolute HTTP or HTTPS URLs.")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var options = ConfigurationOptions.Parse(redisConnectionString);
        options.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(options);
    });

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "eventhub:";
    });
}

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});
#pragma warning restore EXTEXP0018

builder.Services.AddSingleton<ICacheService, HybridCacheService>();
builder.Services.AddSingleton<FailedLoginAttemptTracker>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IJwtProvider, JwtProvider>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ISecureTokenService, SecureTokenService>();
builder.Services.AddSingleton<IEmailVerificationLinkBuilder, EmailVerificationLinkBuilder>();
builder.Services.AddSingleton<IPasswordResetLinkBuilder, PasswordResetLinkBuilder>();

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentStateService, PaymentStateService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IAdminReadRepository, AdminReadRepository>();
builder.Services.AddScoped<IReservationExpirationService, ReservationExpirationService>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IStripeEventRepository, StripeEventRepository>();
builder.Services.AddScoped<IStripeWebhookInboxRepository, StripeWebhookInboxRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
if (builder.Configuration.GetValue<bool>("Stripe:Enabled"))
{
    builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();
}
else
{
    builder.Services.AddScoped<IPaymentGateway, DevelopmentPaymentGateway>();
}

builder.Services.AddHostedService<OutboxProcessorBackgroundService>();
builder.Services.AddHostedService<EventStatusBackgroundService>();
builder.Services.AddHostedService<ReservationExpirationBackgroundService>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateEventCommand).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(CreateEventCommand).Assembly);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventHub API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token as: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseRouting();
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<DistributedRateLimitMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            duration = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = Math.Round(e.Value.Duration.TotalMilliseconds, 2)
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();

public partial class Program;
