using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class DiscussionMessageHideConfiguration : IEntityTypeConfiguration<DiscussionMessageHide>
{
    public void Configure(EntityTypeBuilder<DiscussionMessageHide> builder)
    {
        builder.ToTable("engagement_discussion_hides");

        builder.HasKey(hide => new { hide.MessageId, hide.UserId });

        builder.Property(hide => hide.MessageId).HasColumnName("message_id");
        builder.Property(hide => hide.UserId).HasColumnName("user_id");
        builder.Property(hide => hide.HiddenAtUtc).HasColumnName("hidden_at_utc").IsRequired();

        builder.HasOne<DiscussionMessage>()
            .WithMany()
            .HasForeignKey(hide => hide.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // "What has this person hidden?" is the only question asked of it.
        builder.HasIndex(hide => hide.UserId);
    }
}
