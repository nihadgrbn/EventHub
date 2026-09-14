using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Authentication.Commands.Refresh;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtProvider jwtProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var refreshTokenHash = _jwtProvider.HashRefreshToken(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(
            refreshTokenHash,
            cancellationToken);

        if (user is null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token yanlışdır və ya vaxtı bitib.");
        }

        var accessToken = _jwtProvider.Generate(user);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();

        user.RefreshTokenHash = _jwtProvider.HashRefreshToken(newRefreshToken);
        user.RefreshTokenExpiryTime = _jwtProvider.GetRefreshTokenExpiryTime();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            accessToken,
            newRefreshToken);
    }
}
