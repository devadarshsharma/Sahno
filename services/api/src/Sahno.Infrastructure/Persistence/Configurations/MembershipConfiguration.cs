using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Organisations;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");

        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(membership => membership.OrganisationId)
            .HasColumnName("organisation_id")
            .IsRequired();

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(membership => membership.CanManageFinances)
            .HasColumnName("can_manage_finances")
            .IsRequired();

        builder.Property(membership => membership.JoinedAtUtc)
            .HasColumnName("joined_at_utc")
            .IsRequired();

        builder.Property(membership => membership.SetupChecklistDismissedAtUtc)
            .HasColumnName("setup_checklist_dismissed_at_utc");

        // One membership per person per organisation (D-044).
        builder.HasIndex(membership => new
            {
                membership.OrganisationId,
                membership.UserId,
            })
            .IsUnique();

        // Exactly one Owner per organisation (D-013), enforced by the database.
        builder.HasIndex(membership => membership.OrganisationId)
            .IsUnique()
            .HasFilter("role = 'Owner'")
            .HasDatabaseName("ix_memberships_single_owner");

        builder.HasIndex(membership => membership.UserId);
    }
}
