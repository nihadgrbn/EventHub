namespace EventHub.Application.Common.Interfaces;

public interface IPasswordResetLinkBuilder
{
    string Build(string email, string token);
}
