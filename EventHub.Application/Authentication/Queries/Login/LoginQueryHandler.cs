using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Authentication.Queries.Login
{
    public class LoginQueryHandler : IRequestHandler<LoginQuery, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtProvider _jwtProvider;
        private readonly IUnitOfWork _unitOfWork;

        public LoginQueryHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtProvider jwtProvider,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResponse> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Email ve Password yanlisdir");
            }

            var token = _jwtProvider.Generate(user);
            var refreshToken = _jwtProvider.GenerateRefreshToken();
            user.RefreshTokenHash = _jwtProvider.HashRefreshToken(refreshToken);
            user.RefreshTokenExpiryTime = _jwtProvider.GetRefreshTokenExpiryTime();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthResponse(user.Id, user.FirstName, user.LastName, user.Email, user.Role, token, refreshToken);
        }
    }
}