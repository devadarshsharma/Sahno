using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class DiscussionMessageConfiguration
    : IEntityTypeConfiguration<DiscussionMessage>
{
    public void Configure(EntityTypeBuilder<DiscussionMessage> builder)
    {
        builder.ToTable("engagement_discussion_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(message => message.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        // Nullable because removal drops the text rather than keeping a copy
        // nobody is supposed to read.
        builder.Property(message => message.Body)
            .HasColumnName("body")
            .HasMaxLength(DiscussionMessage.BodyMaxLength);

        builder.Property(message => message.PostedAtUtc)
            .HasColumnName("posted_at_utc")
            .IsRequired();

        builder.Property(message => message.EditedAtUtc)
            .HasColumnName("edited_at_utc");

        builder.Property(message => message.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(message => message.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.HasIndex(message =>
            new { message.EngagementId, message.PostedAtUtc });
    }
}
