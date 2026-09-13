using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahno.Infrastructure.Migrations
{
    /// <summary>
    /// Customers become an organisation-level directory. The per-booking
    /// name/contact/phone/email columns are carried into it first — one
    /// customer per distinct name per organisation, so "Patel family" typed on
    /// three bookings becomes one customer with three bookings — and only then
    /// dropped.
    /// </summary>
    public partial class AddCustomerDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_organisation_id_name",
                table: "customers",
                columns: new[] { "organisation_id", "name" });

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "engagement_customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_engagement_customers_customer_id",
                table: "engagement_customers",
                column: "customer_id");

            // Carry existing names across. The most recently updated booking
            // for a name supplies the contact details; the engagement's
            // creator is recorded as the customer's creator.
            migrationBuilder.Sql("""
                INSERT INTO customers (
                    id, organisation_id, name, contact_name, phone, email, notes,
                    created_by_user_id, created_at_utc, updated_at_utc)
                SELECT DISTINCT ON (e.organisation_id, lower(trim(ec.name)))
                    gen_random_uuid(),
                    e.organisation_id,
                    trim(ec.name),
                    ec.contact_name,
                    ec.phone,
                    ec.email,
                    NULL,
                    e.created_by_user_id,
                    ec.updated_at_utc,
                    ec.updated_at_utc
                FROM engagement_customers ec
                JOIN engagements e ON e.id = ec.engagement_id
                WHERE ec.name IS NOT NULL AND trim(ec.name) <> ''
                ORDER BY e.organisation_id, lower(trim(ec.name)), ec.updated_at_utc DESC;

                UPDATE engagement_customers ec
                SET customer_id = c.id
                FROM engagements e, customers c
                WHERE e.id = ec.engagement_id
                  AND c.organisation_id = e.organisation_id
                  AND ec.name IS NOT NULL
                  AND lower(trim(ec.name)) = lower(c.name);
                """);

            migrationBuilder.DropColumn(
                name: "contact_name",
                table: "engagement_customers");

            migrationBuilder.DropColumn(
                name: "email",
                table: "engagement_customers");

            migrationBuilder.DropColumn(
                name: "name",
                table: "engagement_customers");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "engagement_customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contact_name",
                table: "engagement_customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "engagement_customers",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "engagement_customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "engagement_customers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            // Copy the details back onto each booking before the directory goes.
            migrationBuilder.Sql("""
                UPDATE engagement_customers ec
                SET name = c.name,
                    contact_name = c.contact_name,
                    phone = c.phone,
                    email = c.email
                FROM customers c
                WHERE c.id = ec.customer_id;
                """);

            migrationBuilder.DropIndex(
                name: "IX_engagement_customers_customer_id",
                table: "engagement_customers");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "engagement_customers");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
