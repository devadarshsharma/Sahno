using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEngagementParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "engagement_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    response = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    responded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reminded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    removed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_participants", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_participants_engagement_id_user_id",
                table: "engagement_participants",
                columns: new[] { "engagement_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_engagement_participants_user_id",
                table: "engagement_participants",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "engagement_participants");
        }
    }
}
