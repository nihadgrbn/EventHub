using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(1000);
            builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Address).HasMaxLength(300).IsRequired();
            builder.Property(e => e.PosterImageUrl).HasMaxLength(2048);
            builder.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
            builder.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
            builder.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(EventHub.Domain.Enums.EventStatus.Draft)
                .IsRequired();
            builder.HasOne(e => e.Organizer)
                .WithMany()
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
