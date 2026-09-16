using EventHub.Application.Authentication.Command.Register;
using EventHub.Application.Authentication.Commands.Logout;
using EventHub.Application.Authentication.Commands.Refresh;
using EventHub.Application.Authentication.Queries.Login;
using EventHub.Application.Authentication.Commands.ForgotPassword;
using EventHub.Application.Authentication.Commands.ResetPassword;
using EventHub.Application.Authentication.Commands.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var response = await _sender.Send(command);

        return Ok(new
        {
            Message = "Register successful",
            Data = response
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginQuery query)
    {
        var response = await _sender.Send(query);

        return Ok(new
        {
            Message = "Login successful",
            Data = response
        });
    }

    [HttpPost("refresh")]
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
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await _sender.Send(command);
        return Ok(new { Message = "If the email exists, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await _sender.Send(command);
        return Ok(new { Message = "Password reset successfully." });
    }

    [HttpGet("reset-password-page")]
    public IActionResult ResetPasswordPage([FromQuery] string email, [FromQuery] string token)
    {
        var safeEmail = System.Net.WebUtility.HtmlEncode(email);
        var safeToken = System.Net.WebUtility.HtmlEncode(token);

        return Content($"""
            <!doctype html>
            <html><body>
            <h2>Reset password</h2>
            <form method="post" action="/api/Auth/reset-password-page">
                <input type="hidden" name="email" value="{safeEmail}" />
                <input type="hidden" name="token" value="{safeToken}" />
                <label>New password</label>
                <input type="password" name="newPassword" required minlength="8" />
                <button type="submit">Reset password</button>
            </form>
            </body></html>
            """, "text/html");
    }

    [HttpPost("reset-password-page")]
    public async Task<IActionResult> ResetPasswordPage(
        [FromForm] string email,
        [FromForm] string token,
        [FromForm] string newPassword)
    {
        await _sender.Send(new ResetPasswordCommand(email, token, newPassword));
        return Content("<html><body><h2>Password reset successfully.</h2><p>You can now close this page and log in.</p></body></html>", "text/html");
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command)
    {
        await _sender.Send(command);
        return Ok(new { Message = "Email verified successfully." });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmailFromLink([FromQuery] string token)
    {
        await _sender.Send(new VerifyEmailCommand(token));

        return Content(
            "<html><body><h2>Email verified successfully.</h2><p>You can now close this page and log in.</p></body></html>",
            "text/html");
    }
}