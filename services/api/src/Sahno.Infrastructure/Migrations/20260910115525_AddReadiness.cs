using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "call_time",
                table: "engagements",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dress_notes",
                table: "engagements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "engagement_readiness_waivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    waived_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    waived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_readiness_waivers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_readiness_waivers_engagement_id_item",
                table: "engagement_readiness_waivers",
                columns: new[] { "engagement_id", "item" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "engagement_readiness_waivers");

            migrationBuilder.DropColumn(
                name: "call_time",
                table: "engagements");

            migrationBuilder.DropColumn(
                name: "dress_notes",
                table: "engagements");
        }
    }
}
