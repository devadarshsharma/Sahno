using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsibilitiesRehearsalsAndResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "engagement_rehearsals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    venue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_rehearsals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "engagement_resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_resources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "engagement_responsibilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_done = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_responsibilities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_rehearsals_engagement_id_date",
                table: "engagement_rehearsals",
                columns: new[] { "engagement_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_resources_engagement_id",
                table: "engagement_resources",
                column: "engagement_id");

            migrationBuilder.CreateIndex(
                name: "IX_engagement_responsibilities_assigned_user_id",
                table: "engagement_responsibilities",
                column: "assigned_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_engagement_responsibilities_engagement_id",
                table: "engagement_responsibilities",
                column: "engagement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "engagement_rehearsals");

            migrationBuilder.DropTable(
                name: "engagement_resources");

            migrationBuilder.DropTable(
                name: "engagement_responsibilities");
        }
    }
}
