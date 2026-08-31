using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Organisations;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class OrganisationConfiguration : IEntityTypeConfiguration<Organisation>
{
    public void Configure(EntityTypeBuilder<Organisation> builder)
    {
        builder.ToTable("organisations");

        builder.HasKey(organisation => organisation.Id);

        builder.Property(organisation => organisation.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(organisation => organisation.Name)
            .HasColumnName("name")
            .HasMaxLength(Organisation.NameMaxLength)
            .IsRequired();

        builder.Property(organisation => organisation.GroupType)
            .HasColumnName("group_type")
            .HasMaxLength(Organisation.GroupTypeMaxLength);

        builder.Property(organisation => organisation.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(Organisation.TimeZoneMaxLength)
            .IsRequired();

        builder.Property(organisation => organisation.LogoUrl)
            .HasColumnName("logo_url")
            .HasMaxLength(500);

        builder.Property(organisation => organisation.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
    }
}
