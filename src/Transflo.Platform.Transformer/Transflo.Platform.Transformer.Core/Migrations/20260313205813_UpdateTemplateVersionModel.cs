using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTemplateVersionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_field_mapping_templates_customers_customer_id",
                table: "field_mapping_templates");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropIndex(
                name: "IX_field_mapping_templates_customer_id",
                table: "field_mapping_templates");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "field_mapping_templates");

            migrationBuilder.AddColumn<DateTime>(
                name: "published_at",
                table: "template_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "published_by",
                table: "template_versions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "template_versions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "lookup_tables",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 13, 20, 58, 13, 548, DateTimeKind.Utc).AddTicks(8588), new DateTime(2026, 3, 13, 20, 58, 13, 548, DateTimeKind.Utc).AddTicks(8588) });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 13, 20, 58, 13, 548, DateTimeKind.Utc).AddTicks(8588), new DateTime(2026, 3, 13, 20, 58, 13, 548, DateTimeKind.Utc).AddTicks(8588) });

            migrationBuilder.CreateIndex(
                name: "IX_template_versions_status",
                table: "template_versions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_template_versions_template_id_version",
                table: "template_versions",
                columns: new[] { "template_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_template_versions_status",
                table: "template_versions");

            migrationBuilder.DropIndex(
                name: "IX_template_versions_template_id_version",
                table: "template_versions");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "template_versions");

            migrationBuilder.DropColumn(
                name: "published_by",
                table: "template_versions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "template_versions");

            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "lookup_tables");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "field_mapping_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 11, 15, 16, 10, 223, DateTimeKind.Utc).AddTicks(3190), new DateTime(2026, 3, 11, 15, 16, 10, 223, DateTimeKind.Utc).AddTicks(3190) });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 11, 15, 16, 10, 223, DateTimeKind.Utc).AddTicks(3190), new DateTime(2026, 3, 11, 15, 16, 10, 223, DateTimeKind.Utc).AddTicks(3190) });

            migrationBuilder.CreateIndex(
                name: "IX_field_mapping_templates_customer_id",
                table: "field_mapping_templates",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_code",
                table: "customers",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_is_active",
                table: "customers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_customers_name",
                table: "customers",
                column: "name");

            migrationBuilder.AddForeignKey(
                name: "FK_field_mapping_templates_customers_customer_id",
                table: "field_mapping_templates",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
