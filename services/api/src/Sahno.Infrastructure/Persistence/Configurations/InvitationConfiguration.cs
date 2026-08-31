using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Organisations;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");

        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(invitation => invitation.OrganisationId)
            .HasColumnName("organisation_id")
            .IsRequired();

        builder.Property(invitation => invitation.Token)
            .HasColumnName("token")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(invitation => invitation.Token)
            .IsUnique();

        builder.Property(invitation => invitation.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(invitation => invitation.Email)
            .HasColumnName("email")
            .HasMaxLength(320);

        builder.Property(invitation => invitation.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(invitation => invitation.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(invitation => invitation.ExpiresAtUtc)
            .HasColumnName("expires_at_utc");

        builder.Property(invitation => invitation.RevokedAtUtc)
            .HasColumnName("revoked_at_utc");

        builder.Property(invitation => invitation.AcceptedByUserId)
            .HasColumnName("accepted_by_user_id");

        builder.Property(invitation => invitation.AcceptedAtUtc)
            .HasColumnName("accepted_at_utc");

        builder.HasIndex(invitation => invitation.OrganisationId);
    }
}
