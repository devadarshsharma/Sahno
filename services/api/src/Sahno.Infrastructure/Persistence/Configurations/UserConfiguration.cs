using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Users;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(user => user.ExternalSubject)
            .HasColumnName("external_subject")
            .HasMaxLength(256)
            .IsRequired();

        // One Sahno user per canonical external subject — the database is the
        // final authority against duplicate creation races.
        builder.HasIndex(user => user.ExternalSubject)
            .IsUnique();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320);

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200);

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
    }
}
