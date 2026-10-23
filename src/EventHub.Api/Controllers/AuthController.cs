using EventHub.Api.Constants;
using EventHub.Api.Services;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Authentication.Command.Register;
using EventHub.Application.Authentication.Commands.ForgotPassword;
using EventHub.Application.Authentication.Commands.Logout;
using EventHub.Application.Authentication.Commands.Refresh;
using EventHub.Application.Authentication.Commands.ResetPassword;
using EventHub.Application.Authentication.Commands.VerifyEmail;
using EventHub.Application.Authentication.Queries.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly FailedLoginAttemptTracker _failedLoginAttemptTracker;

    public AuthController(ISender sender, FailedLoginAttemptTracker failedLoginAttemptTracker)
    {
        _sender = sender;
        _failedLoginAttemptTracker = failedLoginAttemptTracker;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        await _sender.Send(command);

        return Ok(new
        {
            Message = "Registration successful. Please check your email to verify your account before logging in."
        });
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Login([FromBody] LoginQuery query)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous-client";
        if (_failedLoginAttemptTracker.TryGetRetryAfter(ipAddress, query.Email, out var retryAfter))
        {
            return TooManyLoginAttempts(retryAfter);
        }

        try
        {
            var response = await _sender.Send(query);
            _failedLoginAttemptTracker.RecordSuccess(ipAddress, query.Email);

            return Ok(new
            {
                Message = "Login successful",
                Data = response
            });
        }
        catch (UnauthorizedException)
        {
            var failureRetryAfter = _failedLoginAttemptTracker.RecordFailure(ipAddress, query.Email);
            if (failureRetryAfter > TimeSpan.Zero)
            {
                return TooManyLoginAttempts(failureRetryAfter);
            }

            throw;
        }
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var response = await _sender.Send(command);

        return Ok(new
        {
            Message = "Token refreshed successfully",
            Data = response
        });
    }

    [HttpPost("logout")]
    [EnableRateLimiting(RateLimitPolicies.Session)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }

    private IActionResult TooManyLoginAttempts(TimeSpan retryAfter)
    {
        var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        Response.Headers.RetryAfter = seconds.ToString();
        return Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too Many Login Attempts",
            detail: "Too many failed login attempts. Please try again later.",
            extensions: new Dictionary<string, object?> { ["retryAfterSeconds"] = seconds });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await _sender.Send(command);
        return Ok(new { Message = "If the email exists, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await _sender.Send(command);
        return Ok(new { Message = "Password reset successfully." });
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command)
    {
        await _sender.Send(command);
        return Ok(new { Message = "Email verified successfully." });
    }
}