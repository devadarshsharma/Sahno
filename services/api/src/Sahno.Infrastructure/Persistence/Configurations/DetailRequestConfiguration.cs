using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class DetailRequestConfiguration : IEntityTypeConfiguration<DetailRequest>
{
    public void Configure(EntityTypeBuilder<DetailRequest> builder)
    {
        builder.ToTable("engagement_detail_requests");

        // One open request per person per detail (D-089).
        builder.HasKey(request => new { request.EngagementId, request.UserId, request.Item });

        builder.Property(request => request.EngagementId).HasColumnName("engagement_id");
        builder.Property(request => request.UserId).HasColumnName("user_id");
        builder.Property(request => request.Item)
            .HasColumnName("item")
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.Property(request => request.RequestedAtUtc).HasColumnName("requested_at_utc").IsRequired();

        builder.HasOne<Engagement>()
            .WithMany()
            .HasForeignKey(request => request.EngagementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
