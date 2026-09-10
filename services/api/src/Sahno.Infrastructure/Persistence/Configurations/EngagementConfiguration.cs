using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class EngagementConfiguration : IEntityTypeConfiguration<Engagement>
{
    public void Configure(EntityTypeBuilder<Engagement> builder)
    {
        builder.ToTable("engagements");

        builder.HasKey(engagement => engagement.Id);

        builder.Property(engagement => engagement.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(engagement => engagement.OrganisationId)
            .HasColumnName("organisation_id")
            .IsRequired();

        builder.Property(engagement => engagement.Title)
            .HasColumnName("title")
            .HasMaxLength(Engagement.TitleMaxLength)
            .IsRequired();

        builder.Property(engagement => engagement.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(engagement => engagement.StartDate)
            .HasColumnName("start_date");

        builder.Property(engagement => engagement.EndDate)
            .HasColumnName("end_date");

        builder.Property(engagement => engagement.StartTime)
            .HasColumnName("start_time");

        builder.Property(engagement => engagement.CallTime)
            .HasColumnName("call_time");

        builder.Property(engagement => engagement.DressNotes)
            .HasColumnName("dress_notes")
            .HasMaxLength(Engagement.DressNotesMaxLength);

        builder.Property(engagement => engagement.Venue)
            .HasColumnName("venue")
            .HasMaxLength(Engagement.VenueMaxLength);

        builder.Property(engagement => engagement.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(engagement => engagement.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // Derived from the status; never stored.
        builder.Ignore(engagement => engagement.IsSharedWithMembers);
        builder.Ignore(engagement => engagement.CanBeDiscarded);
        builder.Ignore(engagement => engagement.CanChangeDateDirectly);

        // Every list and board is scoped to one organisation.
        builder.HasIndex(engagement => engagement.OrganisationId);
    }
}
