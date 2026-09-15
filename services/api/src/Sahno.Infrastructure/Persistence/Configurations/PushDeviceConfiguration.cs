using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Notifications;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class PushDeviceConfiguration : IEntityTypeConfiguration<PushDevice>
{
    public void Configure(EntityTypeBuilder<PushDevice> builder)
    {
        builder.ToTable("push_devices");

        builder.HasKey(device => device.Id);

        builder.Property(device => device.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(device => device.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(device => device.Token)
            .HasColumnName("token")
            .HasMaxLength(PushDevice.TokenMaxLength)
            .IsRequired();

        builder.Property(device => device.Platform)
            .HasColumnName("platform")
            .HasMaxLength(PushDevice.PlatformMaxLength)
            .IsRequired();

        builder.Property(device => device.DeviceName)
            .HasColumnName("device_name")
            .HasMaxLength(PushDevice.DeviceNameMaxLength);

        builder.Property(device => device.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(device => device.LastSeenAtUtc)
            .HasColumnName("last_seen_at_utc")
            .IsRequired();

        builder.Property(device => device.DisabledAtUtc)
            .HasColumnName("disabled_at_utc");

        builder.Property(device => device.DisabledReason)
            .HasColumnName("disabled_reason")
            .HasMaxLength(PushDevice.ReasonMaxLength);

        builder.Ignore(device => device.IsActive);

        // The token is the identity of a phone; one row per token.
        builder.HasIndex(device => device.Token).IsUnique();
        builder.HasIndex(device => device.UserId);
    }
}
