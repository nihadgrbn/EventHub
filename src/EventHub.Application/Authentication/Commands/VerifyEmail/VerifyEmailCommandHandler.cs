using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IUserRepository _users;
    private readonly ISecureTokenService _tokens;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmailCommandHandler(IUserRepository users, ISecureTokenService tokens, IUnitOfWork unitOfWork)
    {
        _users = users;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailVerificationTokenHashAsync(
            _tokens.HashToken(request.Token), cancellationToken);

        if (user is null || user.EmailVerificationTokenExpires is null ||
            user.EmailVerificationTokenExpires <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("The email verification link is invalid or expired.");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpires = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
