namespace EventHub.Application.Common.Interfaces;

public interface ISecureTokenService
{
    string GenerateToken();
    string HashToken(string token);
    DateTime GetPasswordResetTokenExpiry();
    DateTime GetEmailVerificationTokenExpiry();
}
