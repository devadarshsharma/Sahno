using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "availability_nudge",
                table: "organisations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "2d");

            migrationBuilder.AddColumn<string>(
                name: "event_reminder",
                table: "organisations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "morning");

            migrationBuilder.AddColumn<string>(
                name: "rehearsal_reminder",
                table: "organisations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "morning");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "availability_nudge",
                table: "organisations");

            migrationBuilder.DropColumn(
                name: "event_reminder",
                table: "organisations");

            migrationBuilder.DropColumn(
                name: "rehearsal_reminder",
                table: "organisations");
        }
    }
}
