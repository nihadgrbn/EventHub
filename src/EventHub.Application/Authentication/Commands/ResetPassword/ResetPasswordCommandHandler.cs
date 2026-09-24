using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Authentication.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecureTokenService _tokens;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ISecureTokenService tokens,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var token = request.Token.Trim();
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        var tokenHash = _tokens.HashToken(token);

        if (user is null || user.PasswordResetTokenHash != tokenHash ||
            user.PasswordResetTokenExpires is null || user.PasswordResetTokenExpires <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("The reset link is invalid or expired.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpires = null;
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiryTime = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
