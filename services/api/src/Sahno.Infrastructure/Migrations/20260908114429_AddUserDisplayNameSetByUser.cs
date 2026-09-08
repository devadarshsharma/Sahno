using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDisplayNameSetByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "display_name_set_by_user",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_name_set_by_user",
                table: "users");
        }
    }
}
