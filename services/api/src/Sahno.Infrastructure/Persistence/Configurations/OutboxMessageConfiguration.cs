using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Notifications;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.ToEmail)
            .HasColumnName("to_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(message => message.Subject)
            .HasColumnName("subject")
            .HasMaxLength(OutboxMessage.SubjectMaxLength)
            .IsRequired();

        builder.Property(message => message.TextBody)
            .HasColumnName("text_body")
            .IsRequired();

        builder.Property(message => message.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(message => message.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        builder.Property(message => message.LastAttemptAtUtc)
            .HasColumnName("last_attempt_at_utc");

        builder.Property(message => message.SentAtUtc)
            .HasColumnName("sent_at_utc");

        builder.Property(message => message.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(1000);

        // The worker's one query: unsent rows, oldest first.
        builder.HasIndex(message => new { message.SentAtUtc, message.CreatedAtUtc });
    }
}
