using MediatR;

namespace EventHub.Application.Authentication.Command.Register
{
    public record RegisterCommand(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Role) : IRequest;
}
