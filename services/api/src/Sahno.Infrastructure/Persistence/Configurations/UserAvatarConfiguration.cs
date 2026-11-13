using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Users;

namespace Sahno.Infrastructure.Persistence.Configurations;

/// <summary>
/// Profile photos (D-129), kept apart from <c>users</c> so the bytes are read
/// only when a photo is actually asked for. Removed with the account.
/// </summary>
public sealed class UserAvatarConfiguration : IEntityTypeConfiguration<UserAvatar>
{
    public void Configure(EntityTypeBuilder<UserAvatar> builder)
    {
        builder.ToTable("user_avatars");

        builder.HasKey(avatar => avatar.UserId);

        builder.Property(avatar => avatar.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();

        builder.Property(avatar => avatar.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(avatar => avatar.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(avatar => avatar.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserAvatar>(avatar => avatar.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
