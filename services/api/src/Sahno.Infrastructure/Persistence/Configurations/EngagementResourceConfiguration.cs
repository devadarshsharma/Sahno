using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class EngagementResourceConfiguration
    : IEntityTypeConfiguration<EngagementResource>
{
    public void Configure(EntityTypeBuilder<EngagementResource> builder)
    {
        builder.ToTable("engagement_resources");

        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(resource => resource.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(resource => resource.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(resource => resource.Title)
            .HasColumnName("title")
            .HasMaxLength(EngagementResource.TitleMaxLength)
            .IsRequired();

        builder.Property(resource => resource.Body)
            .HasColumnName("body")
            .HasMaxLength(EngagementResource.BodyMaxLength);

        builder.Property(resource => resource.Url)
            .HasColumnName("url")
            .HasMaxLength(EngagementResource.UrlMaxLength);

        // Stored as text rather than an int: a mis-read audience is a privacy
        // failure, and "AdminsOnly" in the table is unambiguous to anyone
        // reading it (D-023).
        builder.Property(resource => resource.Audience)
            .HasColumnName("audience")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(resource => resource.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(resource => resource.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(resource => resource.EngagementId);
    }
}
