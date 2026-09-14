using EventHub.Application.Authentication.Command.Register;
using EventHub.Application.Authentication.Commands.Register;
using EventHub.Application.Authentication.Commands.Logout;
using EventHub.Application.Authentication.Commands.Refresh;
using EventHub.Application.Authentication.Queries.Login;
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
}