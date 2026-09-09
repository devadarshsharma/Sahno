using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class EngagementParticipantConfiguration
    : IEntityTypeConfiguration<EngagementParticipant>
{
    public void Configure(EntityTypeBuilder<EngagementParticipant> builder)
    {
        builder.ToTable("engagement_participants");

        builder.HasKey(participant => participant.Id);

        builder.Property(participant => participant.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(participant => participant.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(participant => participant.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(participant => participant.RequestedAtUtc)
            .HasColumnName("requested_at_utc")
            .IsRequired();

        builder.Property(participant => participant.Response)
            .HasColumnName("response")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(participant => participant.RespondedAtUtc)
            .HasColumnName("responded_at_utc");

        builder.Property(participant => participant.RemindedAtUtc)
            .HasColumnName("reminded_at_utc");

        builder.Property(participant => participant.RemovedAtUtc)
            .HasColumnName("removed_at_utc");

        builder.Ignore(participant => participant.IsActive);
        builder.Ignore(participant => participant.IsOutstanding);

        // One selection per person per engagement: re-selecting someone who was
        // removed restores their row rather than creating a second answer.
        builder.HasIndex(participant => new
            {
                participant.EngagementId,
                participant.UserId,
            })
            .IsUnique();

        builder.HasIndex(participant => participant.UserId);
    }
}
