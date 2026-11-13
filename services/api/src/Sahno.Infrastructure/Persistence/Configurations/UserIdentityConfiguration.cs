using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Users;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("user_identities");

        // A sign-in opens exactly one account; the key is what makes a
        // concurrent first sign-in lose cleanly rather than make two (D-131).
        builder.HasKey(identity => identity.Subject);

        builder.Property(identity => identity.Subject)
            .HasColumnName("subject")
            .HasMaxLength(256);

        builder.Property(identity => identity.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(identity => identity.LinkedAtUtc)
            .HasColumnName("linked_at_utc")
            .IsRequired();

        builder.HasIndex(identity => identity.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(identity => identity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
