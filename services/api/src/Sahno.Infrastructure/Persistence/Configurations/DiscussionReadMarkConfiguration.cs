using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class DiscussionReadMarkConfiguration : IEntityTypeConfiguration<DiscussionReadMark>
{
    public void Configure(EntityTypeBuilder<DiscussionReadMark> builder)
    {
        builder.ToTable("engagement_discussion_reads");

        // One mark per person per chat, moved forward each time they read (D-096).
        builder.HasKey(mark => new { mark.EngagementId, mark.UserId });

        builder.Property(mark => mark.EngagementId).HasColumnName("engagement_id");
        builder.Property(mark => mark.UserId).HasColumnName("user_id");
        builder.Property(mark => mark.ReadAtUtc).HasColumnName("read_at_utc").IsRequired();

        builder.HasOne<Engagement>()
            .WithMany()
            .HasForeignKey(mark => mark.EngagementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
