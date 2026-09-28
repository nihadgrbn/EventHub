using EventHub.Application.Authentication.Commands.ResetPassword;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Authentication;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ISecureTokenService> _tokens = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ResetPasswordCommandHandler CreateHandler() =>
        new(_users.Object, _passwordHasher.Object, _tokens.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        _users.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _tokens.Setup(t => t.HashToken("token")).Returns("hashed-token");

        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new ResetPasswordCommand("test@example.com", "token", "NewPass123!"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TokenMismatch_ThrowsUnauthorizedException()
    {
        var user = new User
        {
            Email = "test@example.com",
            PasswordResetTokenHash = "correct-hash",
            PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1)
        };

        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokens.Setup(t => t.HashToken("wrong-token")).Returns("wrong-hash");

        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new ResetPasswordCommand(user.Email, "wrong-token", "NewPass123!"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorizedException()
    {
        var user = new User
        {
            Email = "test@example.com",
            PasswordResetTokenHash = "hashed-token",
            PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(-5)
        };

        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokens.Setup(t => t.HashToken("valid-token")).Returns("hashed-token");

        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new ResetPasswordCommand(user.Email, "valid-token", "NewPass123!"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesPasswordAndClearsTokens()
    {
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "old-password-hash",
            PasswordResetTokenHash = "hashed-token",
            PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1),
            RefreshTokenHash = "some-refresh-token-hash",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokens.Setup(t => t.HashToken("valid-token")).Returns("hashed-token");
        _passwordHasher.Setup(p => p.Hash("NewSecurePassword123!")).Returns("new-password-hash");

        var handler = CreateHandler();
        await handler.Handle(new ResetPasswordCommand(user.Email, "valid-token", "NewSecurePassword123!"), CancellationToken.None);

        Assert.Equal("new-password-hash", user.PasswordHash);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpires);
        Assert.Null(user.RefreshTokenHash);
        Assert.Null(user.RefreshTokenExpiryTime);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
