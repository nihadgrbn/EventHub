namespace EventHub.Application.Common.Interfaces;

public interface ISecureTokenService
{
    string GenerateToken();
    string HashToken(string token);
    string GenerateTicketQrToken(Guid ticketId);
    DateTime GetPasswordResetTokenExpiry();
    DateTime GetEmailVerificationTokenExpiry();
}
