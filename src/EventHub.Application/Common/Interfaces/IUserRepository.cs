using EventHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Common.Interfaces
{
    public interface IUserRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
        Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken);
        Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken);
        Task<User?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
        Task<User?> GetByEmailVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    }
}
