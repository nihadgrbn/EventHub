using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            builder.Property(u => u.LastName).HasMaxLength(50).IsRequired();
            builder.Property(u => u.Email).HasMaxLength(100).IsRequired();
            builder.Property(u => u.RefreshTokenHash).HasMaxLength(64);
            builder.Property(u => u.PasswordResetTokenHash).HasMaxLength(64);
            builder.Property(u => u.EmailVerificationTokenHash).HasMaxLength(64);

            builder.HasMany(u => u.Purchases)
                .WithOne(p => p.Attendee)
                .HasForeignKey(p => p.AttendeeId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
