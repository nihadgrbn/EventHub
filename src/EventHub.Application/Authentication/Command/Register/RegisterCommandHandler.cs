using EventHub.Application.Authentication;
using EventHub.Application.Authentication.Command.Register;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using EventHub.Domain.Constants;
using MediatR;

namespace EventHub.Application.Authentication.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenService _secureTokenService;
    private readonly IEmailService _emailService;
    private readonly IEmailVerificationLinkBuilder _verificationLinkBuilder;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IUnitOfWork unitOfWork,
        ISecureTokenService secureTokenService,
        IEmailService emailService,
        IEmailVerificationLinkBuilder verificationLinkBuilder)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
        _secureTokenService = secureTokenService;
        _emailService = emailService;
        _verificationLinkBuilder = verificationLinkBuilder;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (!await _userRepository.IsEmailUniqueAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("This email is already registered.");
        }

        var hashedPassword = _passwordHasher.Hash(request.Password);
        var verificationToken = _secureTokenService.GenerateToken();

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = hashedPassword,
            Role = Roles.Normalize(request.Role),
            EmailVerificationTokenHash = _secureTokenService.HashToken(verificationToken),
            EmailVerificationTokenExpires = _secureTokenService.GetEmailVerificationTokenExpiry()
        };

        await _userRepository.AddAsync(user, cancellationToken);
        var token = _jwtProvider.Generate(user);
        var refreshToken = _jwtProvider.GenerateRefreshToken();
        user.RefreshTokenHash = _jwtProvider.HashRefreshToken(refreshToken);
        user.RefreshTokenExpiryTime = _jwtProvider.GetRefreshTokenExpiryTime();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var verificationLink = _verificationLinkBuilder.Build(verificationToken);
        var emailBody = $"<h2>Verify your email</h2><p>Hello {System.Net.WebUtility.HtmlEncode(user.FirstName)},</p><p><a href=\"{verificationLink}\">Verify email</a></p><p>This link expires in 24 hours.</p>";
        await _emailService.SendEmailAsync(user.Email, "EventHub email verification", emailBody);

        return new AuthResponse(user.Id, user.FirstName, user.LastName, user.Email, user.Role, token, refreshToken);
    }
}