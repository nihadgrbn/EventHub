namespace EventHub.Application.Common.Interfaces;

public interface IEmailVerificationLinkBuilder
{
    string Build(string token);
}