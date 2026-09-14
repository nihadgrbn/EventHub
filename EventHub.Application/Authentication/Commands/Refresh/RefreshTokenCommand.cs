using MediatR;

namespace EventHub.Application.Authentication.Commands.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
