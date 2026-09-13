using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepertoire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pieces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    attribution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    language = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    lyrics = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pieces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "piece_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    piece_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_piece_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_piece_links_pieces_piece_id",
                        column: x => x.piece_id,
                        principalTable: "pieces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "set_list_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    piece_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    added_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_set_list_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_set_list_entries_pieces_piece_id",
                        column: x => x.piece_id,
                        principalTable: "pieces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_piece_links_piece_id",
                table: "piece_links",
                column: "piece_id");

            migrationBuilder.CreateIndex(
                name: "IX_pieces_organisation_id_title",
                table: "pieces",
                columns: new[] { "organisation_id", "title" });

            migrationBuilder.CreateIndex(
                name: "IX_set_list_entries_engagement_id_position",
                table: "set_list_entries",
                columns: new[] { "engagement_id", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_set_list_entries_piece_id",
                table: "set_list_entries",
                column: "piece_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "piece_links");

            migrationBuilder.DropTable(
                name: "set_list_entries");

            migrationBuilder.DropTable(
                name: "pieces");
        }
    }
}
