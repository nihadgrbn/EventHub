using MediatR;

namespace EventHub.Application.Authentication.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;
