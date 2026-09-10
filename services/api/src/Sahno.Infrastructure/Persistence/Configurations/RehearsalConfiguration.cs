using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class RehearsalConfiguration : IEntityTypeConfiguration<Rehearsal>
{
    public void Configure(EntityTypeBuilder<Rehearsal> builder)
    {
        builder.ToTable("engagement_rehearsals");

        builder.HasKey(rehearsal => rehearsal.Id);

        builder.Property(rehearsal => rehearsal.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(rehearsal => rehearsal.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(rehearsal => rehearsal.Title)
            .HasColumnName("title")
            .HasMaxLength(Rehearsal.TitleMaxLength);

        builder.Property(rehearsal => rehearsal.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(rehearsal => rehearsal.StartTime)
            .HasColumnName("start_time");

        builder.Property(rehearsal => rehearsal.EndTime)
            .HasColumnName("end_time");

        builder.Property(rehearsal => rehearsal.Venue)
            .HasColumnName("venue")
            .HasMaxLength(Rehearsal.VenueMaxLength);

        builder.Property(rehearsal => rehearsal.Notes)
            .HasColumnName("notes")
            .HasMaxLength(Rehearsal.NotesMaxLength);

        builder.Property(rehearsal => rehearsal.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(rehearsal => rehearsal.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(rehearsal => new { rehearsal.EngagementId, rehearsal.Date });
    }
}
