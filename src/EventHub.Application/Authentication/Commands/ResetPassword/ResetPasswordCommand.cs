using MediatR;

namespace EventHub.Application.Authentication.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest;
