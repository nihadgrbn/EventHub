using EventHub.Infrastructure.Authentication;
using Xunit;

namespace EventHub.Infrastructure.Tests.Authentication;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_GeneratesNonEmptyDifferentHashesForSamePassword()
    {
        const string password = "SecurePassword123!";
        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash1));
        Assert.False(string.IsNullOrWhiteSpace(hash2));
        Assert.NotEqual(hash1, hash2); // BCrypt uses unique salts each time
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        const string password = "MySecretPassword!";
        var hash = _hasher.Hash(password);

        var result = _hasher.Verify(password, hash);
        Assert.True(result);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        const string password = "MySecretPassword!";
        var hash = _hasher.Hash(password);

        var result = _hasher.Verify("IncorrectPassword", hash);
        Assert.False(result);
    }
}
