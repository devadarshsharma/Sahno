using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPushNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "to_email",
                table: "outbox_messages",
                newName: "recipient");

            migrationBuilder.AddColumn<string>(
                name: "channel",
                table: "outbox_messages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "data_json",
                table: "outbox_messages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "push_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    device_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    disabled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disabled_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_devices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_push_devices_token",
                table: "push_devices",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_devices_user_id",
                table: "push_devices",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "push_devices");

            migrationBuilder.DropColumn(
                name: "channel",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "data_json",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "recipient",
                table: "outbox_messages",
                newName: "to_email");
        }
    }
}
