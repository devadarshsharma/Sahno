using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_identities",
                columns: table => new
                {
                    subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_identities", x => x.subject);
                    table.ForeignKey(
                        name: "FK_user_identities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_identities_user_id",
                table: "user_identities",
                column: "user_id");

            // Every account keeps the sign-in it was made with.
            migrationBuilder.Sql("""
                INSERT INTO user_identities (subject, user_id, linked_at_utc)
                SELECT external_subject, id, created_at_utc FROM users
                ON CONFLICT (subject) DO NOTHING;
                """);

            // Accounts made by signing in a second way before D-131 — an
            // emailed code to the address a Google account already had — and
            // never used for anything: in no organisation, now or before. Their
            // sign-in moves to the older account with that address, and the
            // empty one goes; if anything still points at it, it stays, with no
            // way left to sign in to it.
            migrationBuilder.Sql("""
                DO $$
                DECLARE duplicate RECORD;
                BEGIN
                  FOR duplicate IN
                    SELECT d.id AS duplicate_id,
                           (SELECT o.id FROM users o
                             WHERE lower(o.email) = lower(d.email) AND o.created_at_utc < d.created_at_utc
                             ORDER BY o.created_at_utc LIMIT 1) AS owner_id
                    FROM users d
                    WHERE d.email IS NOT NULL
                      AND (d.external_subject LIKE 'email|%' OR d.external_subject LIKE 'google-oauth2|%')
                      AND NOT EXISTS (SELECT 1 FROM memberships m WHERE m.user_id = d.id)
                  LOOP
                    CONTINUE WHEN duplicate.owner_id IS NULL;
                    UPDATE user_identities SET user_id = duplicate.owner_id WHERE user_id = duplicate.duplicate_id;
                    BEGIN
                      DELETE FROM users WHERE id = duplicate.duplicate_id;
                    EXCEPTION WHEN foreign_key_violation THEN
                      NULL;
                    END;
                  END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_identities");
        }
    }
}
