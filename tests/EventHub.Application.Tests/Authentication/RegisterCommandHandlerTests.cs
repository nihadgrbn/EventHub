using EventHub.Application.Authentication.Command.Register;
using EventHub.Application.Authentication.Commands.Register;
using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Constants;
using EventHub.Domain.Entities;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Authentication;

public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISecureTokenService> _tokens = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IEmailVerificationLinkBuilder> _linkBuilder = new();

    private RegisterCommandHandler CreateHandler() =>
        new(_users.Object, _hasher.Object, _unitOfWork.Object, _tokens.Object, _emailService.Object, _linkBuilder.Object);

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsConflictException()
    {
        _users.Setup(u => u.IsEmailUniqueAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var command = new RegisterCommand("First", "Last", "existing@example.com", "Password123!", "Attendee");

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UniqueEmail_CreatesUserHashesPasswordAndSendsVerificationEmail()
    {
        _users.Setup(u => u.IsEmailUniqueAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _hasher.Setup(h => h.Hash("Password123!")).Returns("hashed-pw");
        _tokens.Setup(t => t.GenerateToken()).Returns("verification-raw-token");
        _tokens.Setup(t => t.HashToken("verification-raw-token")).Returns("verification-hash");
        _tokens.Setup(t => t.GetEmailVerificationTokenExpiry()).Returns(DateTime.UtcNow.AddHours(24));
        _linkBuilder.Setup(l => l.Build("verification-raw-token")).Returns("http://localhost:8080/api/Auth/verify-email?token=verification-raw-token");

        User? capturedUser = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new RegisterCommand("Alice", "Smith", "new@example.com", "Password123!", "Organizer");
        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedUser);
        Assert.Equal("Alice", capturedUser.FirstName);
        Assert.Equal("Smith", capturedUser.LastName);
        Assert.Equal("new@example.com", capturedUser.Email);
        Assert.Equal("hashed-pw", capturedUser.PasswordHash);
        Assert.Equal(Roles.Organizer, capturedUser.Role);
        Assert.Equal("verification-hash", capturedUser.EmailVerificationTokenHash);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(e => e.SendEmailAsync(
            "new@example.com",
            "EventHub email verification",
            It.Is<string>(b => b.Contains("Alice")),
            true,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
