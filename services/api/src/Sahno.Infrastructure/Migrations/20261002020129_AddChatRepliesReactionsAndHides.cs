using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRepliesReactionsAndHides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reply_to_message_id",
                table: "engagement_discussion_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "engagement_discussion_hides",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hidden_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_discussion_hides", x => new { x.message_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_engagement_discussion_hides_engagement_discussion_messages_~",
                        column: x => x.message_id,
                        principalTable: "engagement_discussion_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "engagement_discussion_reactions",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reacted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_discussion_reactions", x => new { x.message_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_engagement_discussion_reactions_engagement_discussion_messa~",
                        column: x => x.message_id,
                        principalTable: "engagement_discussion_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_discussion_hides_user_id",
                table: "engagement_discussion_hides",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_engagement_discussion_reactions_engagement_id",
                table: "engagement_discussion_reactions",
                column: "engagement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "engagement_discussion_hides");

            migrationBuilder.DropTable(
                name: "engagement_discussion_reactions");

            migrationBuilder.DropColumn(
                name: "reply_to_message_id",
                table: "engagement_discussion_messages");
        }
    }
}
