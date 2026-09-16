using MediatR;

namespace EventHub.Application.Authentication.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : IRequest;
