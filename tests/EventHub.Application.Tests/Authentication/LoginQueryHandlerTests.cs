using EventHub.Application.Authentication.Queries.Login;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Authentication;

public sealed class LoginQueryHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtProvider> _jwt = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LoginQueryHandler CreateHandler() =>
        new(_users.Object, _hasher.Object, _jwt.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        _users.Setup(u => u.GetByEmailAsync("notfound@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new LoginQuery("notfound@example.com", "Password123!"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsUnauthorizedException()
    {
        var user = new User
        {
            Email = "user@example.com",
            PasswordHash = "correct-hash",
            EmailVerificationTokenHash = null
        };

        _users.Setup(u => u.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("WrongPass", "correct-hash")).Returns(false);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new LoginQuery(user.Email, "WrongPass"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailNotVerified_ThrowsUnauthorizedException()
    {
        var user = new User
        {
            Email = "user@example.com",
            PasswordHash = "correct-hash",
            EmailVerificationTokenHash = "pending-verification-token"
        };

        _users.Setup(u => u.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("CorrectPass", "correct-hash")).Returns(true);

        var handler = CreateHandler();
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new LoginQuery(user.Email, "CorrectPass"), CancellationToken.None));

        Assert.Contains("verify your email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ValidCredentialsAndVerifiedEmail_ReturnsAuthResponseAndSavesRefreshToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "Valid",
            LastName = "User",
            Role = "Attendee",
            PasswordHash = "correct-hash",
            IsEmailVerified = true,
            EmailVerificationTokenHash = null
        };

        _users.Setup(u => u.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("CorrectPass", "correct-hash")).Returns(true);
        _jwt.Setup(j => j.Generate(user)).Returns("valid-jwt");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");
        _jwt.Setup(j => j.HashRefreshToken("new-refresh-token")).Returns("hashed-refresh-token");
        _jwt.Setup(j => j.GetRefreshTokenExpiryTime()).Returns(DateTime.UtcNow.AddDays(7));

        var handler = CreateHandler();
        var response = await handler.Handle(new LoginQuery(user.Email, "CorrectPass"), CancellationToken.None);

        Assert.Equal("valid-jwt", response.Token);
        Assert.Equal("new-refresh-token", response.RefreshToken);
        Assert.Equal("Attendee", response.Role);
        Assert.Equal("hashed-refresh-token", user.RefreshTokenHash);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
