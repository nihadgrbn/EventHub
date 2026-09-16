using MediatR;

namespace EventHub.Application.Authentication.Queries.Login
{
    public record LoginQuery(
    string Email,
    string Password
) : IRequest<AuthResponse>;
}
