using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;
using Sahno.Domain.Repertoire;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class PieceConfiguration : IEntityTypeConfiguration<Piece>
{
    public void Configure(EntityTypeBuilder<Piece> builder)
    {
        builder.ToTable("pieces");

        builder.HasKey(piece => piece.Id);

        builder.Property(piece => piece.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(piece => piece.OrganisationId)
            .HasColumnName("organisation_id")
            .IsRequired();

        builder.Property(piece => piece.Title)
            .HasColumnName("title")
            .HasMaxLength(Piece.TitleMaxLength)
            .IsRequired();

        builder.Property(piece => piece.Attribution)
            .HasColumnName("attribution")
            .HasMaxLength(Piece.AttributionMaxLength);

        builder.Property(piece => piece.Language)
            .HasColumnName("language")
            .HasMaxLength(Piece.LanguageMaxLength);

        builder.Property(piece => piece.Key)
            .HasColumnName("key")
            .HasMaxLength(Piece.KeyMaxLength);

        builder.Property(piece => piece.DurationMinutes)
            .HasColumnName("duration_minutes");

        builder.Property(piece => piece.Lyrics)
            .HasColumnName("lyrics")
            .HasMaxLength(Piece.LyricsMaxLength);

        builder.Property(piece => piece.Notes)
            .HasColumnName("notes")
            .HasMaxLength(Piece.NotesMaxLength);

        builder.Property(piece => piece.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(piece => piece.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(piece => piece.UpdatedByUserId)
            .HasColumnName("updated_by_user_id")
            .IsRequired();

        builder.Property(piece => piece.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Ignore(piece => piece.HasLyrics);

        builder.HasIndex(piece => new { piece.OrganisationId, piece.Title });
    }
}

public sealed class PieceLinkConfiguration : IEntityTypeConfiguration<PieceLink>
{
    public void Configure(EntityTypeBuilder<PieceLink> builder)
    {
        builder.ToTable("piece_links");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(link => link.PieceId)
            .HasColumnName("piece_id")
            .IsRequired();

        builder.Property(link => link.Title)
            .HasColumnName("title")
            .HasMaxLength(PieceLink.TitleMaxLength)
            .IsRequired();

        builder.Property(link => link.Url)
            .HasColumnName("url")
            .HasMaxLength(PieceLink.UrlMaxLength)
            .IsRequired();

        builder.Property(link => link.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(link => link.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<Piece>()
            .WithMany()
            .HasForeignKey(link => link.PieceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => link.PieceId);
    }
}

public sealed class SetListEntryConfiguration : IEntityTypeConfiguration<SetListEntry>
{
    public void Configure(EntityTypeBuilder<SetListEntry> builder)
    {
        builder.ToTable("set_list_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entry => entry.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(entry => entry.PieceId)
            .HasColumnName("piece_id")
            .IsRequired();

        builder.Property(entry => entry.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Property(entry => entry.Note)
            .HasColumnName("note")
            .HasMaxLength(SetListEntry.NoteMaxLength);

        builder.Property(entry => entry.AddedByUserId)
            .HasColumnName("added_by_user_id")
            .IsRequired();

        builder.Property(entry => entry.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // A deleted piece takes its entries with it (RepertoireService.DeleteAsync).
        builder.HasOne<Piece>()
            .WithMany()
            .HasForeignKey(entry => entry.PieceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entry => new { entry.EngagementId, entry.Position });
        builder.HasIndex(entry => entry.PieceId);
    }
}
