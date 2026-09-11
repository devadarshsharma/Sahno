using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Notifications;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(notification => notification.OrganisationId)
            .HasColumnName("organisation_id")
            .IsRequired();

        builder.Property(notification => notification.RecipientUserId)
            .HasColumnName("recipient_user_id")
            .IsRequired();

        builder.Property(notification => notification.EngagementId)
            .HasColumnName("engagement_id");

        builder.Property(notification => notification.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasColumnName("title")
            .HasMaxLength(Notification.TitleMaxLength)
            .IsRequired();

        builder.Property(notification => notification.Body)
            .HasColumnName("body")
            .HasMaxLength(Notification.BodyMaxLength);

        builder.Property(notification => notification.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(notification => notification.ReadAtUtc)
            .HasColumnName("read_at_utc");

        // The bell's two queries: the list, newest first, and the unread count.
        builder.HasIndex(notification => new
        {
            notification.OrganisationId,
            notification.RecipientUserId,
            notification.CreatedAtUtc,
        });
    }
}
