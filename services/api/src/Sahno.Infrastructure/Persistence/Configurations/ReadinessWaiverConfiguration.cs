using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class ReadinessWaiverConfiguration
    : IEntityTypeConfiguration<ReadinessWaiver>
{
    public void Configure(EntityTypeBuilder<ReadinessWaiver> builder)
    {
        builder.ToTable("engagement_readiness_waivers");

        builder.HasKey(waiver => waiver.Id);

        builder.Property(waiver => waiver.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(waiver => waiver.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(waiver => waiver.Item)
            .HasColumnName("item")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(waiver => waiver.WaivedByUserId)
            .HasColumnName("waived_by_user_id")
            .IsRequired();

        builder.Property(waiver => waiver.WaivedAtUtc)
            .HasColumnName("waived_at_utc")
            .IsRequired();

        // One waiver per item per engagement: waiving twice is the same fact.
        builder.HasIndex(waiver => new { waiver.EngagementId, waiver.Item })
            .IsUnique();
    }
}
