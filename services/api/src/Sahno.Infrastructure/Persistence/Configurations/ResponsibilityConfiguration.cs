using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class ResponsibilityConfiguration
    : IEntityTypeConfiguration<Responsibility>
{
    public void Configure(EntityTypeBuilder<Responsibility> builder)
    {
        builder.ToTable("engagement_responsibilities");

        builder.HasKey(responsibility => responsibility.Id);

        builder.Property(responsibility => responsibility.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(responsibility => responsibility.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(responsibility => responsibility.Title)
            .HasColumnName("title")
            .HasMaxLength(Responsibility.TitleMaxLength)
            .IsRequired();

        builder.Property(responsibility => responsibility.Detail)
            .HasColumnName("detail")
            .HasMaxLength(Responsibility.DetailMaxLength);

        builder.Property(responsibility => responsibility.AssignedUserId)
            .HasColumnName("assigned_user_id");

        builder.Property(responsibility => responsibility.IsDone)
            .HasColumnName("is_done")
            .IsRequired();

        builder.Property(responsibility => responsibility.Note)
            .HasColumnName("note")
            .HasMaxLength(Responsibility.NoteMaxLength);

        builder.Property(responsibility => responsibility.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(responsibility => responsibility.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(responsibility => responsibility.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.HasIndex(responsibility => responsibility.EngagementId);

        // A member's own list of jobs across every event they are on.
        builder.HasIndex(responsibility => responsibility.AssignedUserId);
    }
}
