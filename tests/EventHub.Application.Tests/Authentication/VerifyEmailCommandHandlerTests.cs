using EventHub.Application.Authentication.Commands.VerifyEmail;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Authentication;

public sealed class VerifyEmailCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISecureTokenService> _tokens = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private VerifyEmailCommandHandler CreateHandler() =>
        new(_users.Object, _tokens.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        _tokens.Setup(t => t.HashToken("raw-token")).Returns("hashed-token");
        _users.Setup(u => u.GetByEmailVerificationTokenHashAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TokenExpired_ThrowsUnauthorizedException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            EmailVerificationTokenHash = "hashed-token",
            EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(-1)
        };

        _tokens.Setup(t => t.HashToken("raw-token")).Returns("hashed-token");
        _users.Setup(u => u.GetByEmailVerificationTokenHashAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidToken_VerifiesEmailAndClearsTokens()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            IsEmailVerified = false,
            EmailVerificationTokenHash = "hashed-token",
            EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24)
        };

        _tokens.Setup(t => t.HashToken("raw-token")).Returns("hashed-token");
        _users.Setup(u => u.GetByEmailVerificationTokenHashAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        await handler.Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        Assert.True(user.IsEmailVerified);
        Assert.Null(user.EmailVerificationTokenHash);
        Assert.Null(user.EmailVerificationTokenExpires);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
