using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "function",
                table: "memberships",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "internal_notes",
                table: "memberships",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "shares_contact_details",
                table: "memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "users");

            migrationBuilder.DropColumn(
                name: "function",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "internal_notes",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "shares_contact_details",
                table: "memberships");
        }
    }
}
