using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Notifications;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class ReminderLogConfiguration : IEntityTypeConfiguration<ReminderLog>
{
    public void Configure(EntityTypeBuilder<ReminderLog> builder)
    {
        builder.ToTable("reminder_log");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(log => log.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(log => log.SubjectId)
            .HasColumnName("subject_id")
            .IsRequired();

        builder.Property(log => log.OccasionDate)
            .HasColumnName("occasion_date")
            .IsRequired();

        builder.Property(log => log.SentAtUtc)
            .HasColumnName("sent_at_utc")
            .IsRequired();

        // The guarantee itself: one reminder per subject per local day, even
        // if two scheduler passes ever raced.
        builder.HasIndex(log => new { log.Kind, log.SubjectId, log.OccasionDate })
            .IsUnique();
    }
}
