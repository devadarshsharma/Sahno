using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganisations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    can_manage_finances = table.Column<bool>(type: "boolean", nullable: false),
                    joined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    setup_checklist_dismissed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memberships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organisations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    group_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organisations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_organisation_id",
                table: "invitations",
                column: "organisation_id");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_token",
                table: "invitations",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_memberships_organisation_id_user_id",
                table: "memberships",
                columns: new[] { "organisation_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_memberships_single_owner",
                table: "memberships",
                column: "organisation_id",
                unique: true,
                filter: "role = 'Owner'");

            migrationBuilder.CreateIndex(
                name: "IX_memberships_user_id",
                table: "memberships",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropTable(
                name: "memberships");

            migrationBuilder.DropTable(
                name: "organisations");
        }
    }
}
