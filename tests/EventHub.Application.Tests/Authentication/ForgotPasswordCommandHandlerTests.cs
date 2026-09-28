using EventHub.Application.Authentication.Commands.ForgotPassword;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Authentication;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISecureTokenService> _tokens = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IPasswordResetLinkBuilder> _resetLinkBuilder = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ForgotPasswordCommandHandler CreateHandler() =>
        new(_users.Object, _tokens.Object, _emailService.Object, _resetLinkBuilder.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_UserNotFound_DoesNotSendEmailAndSilentlyCompletes()
    {
        _users.Setup(r => r.GetByEmailAsync("nonexistent@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        await handler.Handle(new ForgotPasswordCommand("nonexistent@example.com"), CancellationToken.None);

        _tokens.Verify(t => t.GenerateToken(), Times.Never);
        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), null, It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserFound_GeneratesTokenSavesExpiryAndSendsEmail()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        const string rawToken = "raw-reset-token";
        const string tokenHash = "hashed-reset-token";
        var expiry = DateTime.UtcNow.AddHours(2);
        const string expectedLink = "http://localhost:3000/reset-password?email=user%40example.com&token=raw-reset-token";

        _tokens.Setup(t => t.GenerateToken()).Returns(rawToken);
        _tokens.Setup(t => t.HashToken(rawToken)).Returns(tokenHash);
        _tokens.Setup(t => t.GetPasswordResetTokenExpiry()).Returns(expiry);
        _resetLinkBuilder.Setup(b => b.Build(user.Email, rawToken)).Returns(expectedLink);

        var handler = CreateHandler();
        await handler.Handle(new ForgotPasswordCommand(user.Email), CancellationToken.None);

        Assert.Equal(tokenHash, user.PasswordResetTokenHash);
        Assert.Equal(expiry, user.PasswordResetTokenExpires);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(e => e.SendEmailAsync(
            user.Email,
            "EventHub - Password reset",
            It.Is<string>(body => body.Contains(expectedLink) && body.Contains("John")),
            true,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
