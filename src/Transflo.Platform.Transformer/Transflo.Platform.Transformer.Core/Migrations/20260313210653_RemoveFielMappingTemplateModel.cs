using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFielMappingTemplateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "field_mapping_templates");

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 13, 21, 6, 53, 498, DateTimeKind.Utc).AddTicks(69), new DateTime(2026, 3, 13, 21, 6, 53, 498, DateTimeKind.Utc).AddTicks(69) });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 13, 21, 6, 53, 498, DateTimeKind.Utc).AddTicks(69), new DateTime(2026, 3, 13, 21, 6, 53, 498, DateTimeKind.Utc).AddTicks(69) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "field_mapping_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tms_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    sample_input_json = table.Column<string>(type: "jsonb", nullable: true),
                    source_schema = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    target_schema = table.Column<string>(type: "jsonb", nullable: true),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_mapping_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_field_mapping_templates_tms_systems_tms_system_id",
                        column: x => x.tms_system_id,
                        principalTable: "tms_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_field_mapping_templates_status",
                table: "field_mapping_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_field_mapping_templates_template_id",
                table: "field_mapping_templates",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_mapping_templates_template_id_version",
                table: "field_mapping_templates",
                columns: new[] { "template_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_field_mapping_templates_tms_system_id",
                table: "field_mapping_templates",
                column: "tms_system_id");
        }
    }
}
