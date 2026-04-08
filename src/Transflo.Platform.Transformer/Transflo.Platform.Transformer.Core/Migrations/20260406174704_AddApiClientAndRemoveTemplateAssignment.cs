using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddApiClientAndRemoveTemplateAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"api_client_template_versions\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"api_clients\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"template_assignments\" CASCADE;");


            migrationBuilder.CreateTable(
                name: "api_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_client_template_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    api_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_client_template_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_api_client_template_versions_api_clients_api_client_id",
                        column: x => x.api_client_id,
                        principalTable: "api_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_api_client_template_versions_template_versions_template_ver~",
                        column: x => x.template_version_id,
                        principalTable: "template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateIndex(
                name: "IX_api_client_template_versions_api_client_id",
                table: "api_client_template_versions",
                column: "api_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_client_template_versions_api_client_id_template_version~",
                table: "api_client_template_versions",
                columns: new[] { "api_client_id", "template_version_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_client_template_versions_template_version_id",
                table: "api_client_template_versions",
                column: "template_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_clients_is_active",
                table: "api_clients",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_api_clients_name",
                table: "api_clients",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_client_template_versions");

            migrationBuilder.DropTable(
                name: "api_clients");

            migrationBuilder.DeleteData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("a2c8e4d6-1f3b-4a7c-8e9d-5b0c2f6a4e83"));

            migrationBuilder.DeleteData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("b5f3a9c2-7d4e-4f8b-9a1c-3e6d2b0f8c47"));


            migrationBuilder.CreateTable(
                name: "template_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_assignments_partners_source_partner_id",
                        column: x => x.source_partner_id,
                        principalTable: "partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_template_assignments_partners_target_partner_id",
                        column: x => x.target_partner_id,
                        principalTable: "partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_template_assignments_template_versions_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tms_systems",
                columns: new[] { "id", "connection_config", "created_at", "created_by", "deleted_at", "description", "display_name", "is_active", "is_deleted", "metadata", "name", "revision", "sample_json_schema", "updated_at", "updated_by", "version" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), null, new DateTime(2026, 3, 27, 12, 50, 16, 642, DateTimeKind.Utc).AddTicks(6230), "System", null, "TruckMate Transportation Management System", "TruckMate TMS", true, false, null, "TruckMate", 1, null, new DateTime(2026, 3, 27, 12, 50, 16, 642, DateTimeKind.Utc).AddTicks(6230), null, "1.0" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), null, new DateTime(2026, 3, 27, 12, 50, 16, 642, DateTimeKind.Utc).AddTicks(6230), "System", null, "McLeod Transportation Management System", "McLeod Software", true, false, null, "McLeod", 1, null, new DateTime(2026, 3, 27, 12, 50, 16, 642, DateTimeKind.Utc).AddTicks(6230), null, "1.0" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_template_assignments_source_partner_id",
                table: "template_assignments",
                column: "source_partner_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_assignments_target_partner_id",
                table: "template_assignments",
                column: "target_partner_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_assignments_template_version_id",
                table: "template_assignments",
                column: "template_version_id");
        }
    }
}
