using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class DiscussionReactionConfiguration : IEntityTypeConfiguration<DiscussionReaction>
{
    public void Configure(EntityTypeBuilder<DiscussionReaction> builder)
    {
        builder.ToTable("engagement_discussion_reactions");

        // One reaction per person per message (D-086).
        builder.HasKey(reaction => new { reaction.MessageId, reaction.UserId });

        builder.Property(reaction => reaction.MessageId).HasColumnName("message_id");
        builder.Property(reaction => reaction.EngagementId).HasColumnName("engagement_id").IsRequired();
        builder.Property(reaction => reaction.UserId).HasColumnName("user_id");
        builder.Property(reaction => reaction.Emoji).HasColumnName("emoji").HasMaxLength(16).IsRequired();
        builder.Property(reaction => reaction.ReactedAtUtc).HasColumnName("reacted_at_utc").IsRequired();

        builder.HasOne<DiscussionMessage>()
            .WithMany()
            .HasForeignKey(reaction => reaction.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(reaction => reaction.EngagementId);
    }
}
