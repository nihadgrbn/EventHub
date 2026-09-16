using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
    {
        public void Configure(EntityTypeBuilder<TicketType> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name).HasMaxLength(100).IsRequired();

            builder.Property(t => t.Price).HasColumnType("decimal(18,2)");

            builder.HasOne(t => t.Event)
                   .WithMany(e => e.TicketTypes)
                   .HasForeignKey(t => t.EventId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.Property(t => t.AvailableQuantity)
                .IsConcurrencyToken();
        }
    }
}
