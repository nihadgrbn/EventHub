using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Authentication.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        IJwtProvider jwtProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenHash = _jwtProvider.HashRefreshToken(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(
            refreshTokenHash,
            cancellationToken);

        if (user is null)
        {
            return;
        }

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiryTime = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
