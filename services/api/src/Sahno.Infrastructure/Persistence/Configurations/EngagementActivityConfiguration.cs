using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class EngagementActivityConfiguration
    : IEntityTypeConfiguration<EngagementActivity>
{
    public void Configure(EntityTypeBuilder<EngagementActivity> builder)
    {
        builder.ToTable("engagement_activities");

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(activity => activity.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(activity => activity.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(activity => activity.FromStatus)
            .HasColumnName("from_status")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(activity => activity.ToStatus)
            .HasColumnName("to_status")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(activity => activity.FromStartDate)
            .HasColumnName("from_start_date");

        builder.Property(activity => activity.ToStartDate)
            .HasColumnName("to_start_date");

        builder.Property(activity => activity.Reason)
            .HasColumnName("reason")
            .HasMaxLength(EngagementActivity.ReasonMaxLength);

        builder.Property(activity => activity.ActorUserId)
            .HasColumnName("actor_user_id")
            .IsRequired();

        builder.Property(activity => activity.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.HasIndex(activity => activity.EngagementId);
    }
}
