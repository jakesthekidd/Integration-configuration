using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "partners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partners", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    source_schema = table.Column<string>(type: "jsonb", nullable: true),
                    target_schema = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tms_systems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sample_json_schema = table.Column<string>(type: "jsonb", nullable: true),
                    connection_config = table.Column<string>(type: "jsonb", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tms_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transformation_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input_data = table.Column<string>(type: "jsonb", nullable: true),
                    output_data = table.Column<string>(type: "jsonb", nullable: true),
                    errors = table.Column<string>(type: "jsonb", nullable: true),
                    execution_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transformation_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "template_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    validation_rules = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_versions_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_mapping_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    tms_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    source_schema = table.Column<string>(type: "jsonb", nullable: true),
                    target_schema = table.Column<string>(type: "jsonb", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sample_input_json = table.Column<string>(type: "jsonb", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_mapping_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_field_mapping_templates_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_field_mapping_templates_tms_systems_tms_system_id",
                        column: x => x.tms_system_id,
                        principalTable: "tms_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lookup_tables",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tms_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    mappings = table.Column<string>(type: "jsonb", nullable: true),
                    default_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_case_sensitive = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_tables", x => x.id);
                    table.ForeignKey(
                        name: "FK_lookup_tables_partners_partner_id",
                        column: x => x.partner_id,
                        principalTable: "partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_lookup_tables_tms_systems_tms_system_id",
                        column: x => x.tms_system_id,
                        principalTable: "tms_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    target_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    transformation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transformation_config = table.Column<string>(type: "jsonb", nullable: true),
                    execution_order = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<string>(type: "text", nullable: true),
                    validation_rules = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_field_mappings_template_versions_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
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
                columns: new[] { "id", "connection_config", "created_at", "created_by", "deleted_at", "description", "display_name", "is_active", "metadata", "name", "revision", "sample_json_schema", "updated_at", "updated_by", "version" },
                values: new object[,]
                {
                    { new Guid("b5f3a9c2-7d4e-4f8b-9a1c-3e6d2b0f8c47"), null, new DateTime(2026, 3, 11, 15, 16, 10, 223, DateTimeKind.Utc).AddTicks(3190), "System", null, "TruckMate Transportation Management System", "TruckMate TMS", true, null, "TruckMate", 1, null, new DateTime(2026, 3, 11, 15, 16, 10, 223, DateTimeKind.Utc).AddTicks(3190), null, "1.0" },
                    { new Guid("a2c8e4d6-1f3b-4a7c-8e9d-5b0c2f6a4e83"), null, new DateTime(2026, 3, 11, 15, 16, 10, 223, DateTimeKind.Utc).AddTicks(3190), "System", null, "McLeod Transportation Management System", "McLeod Software", true, null, "McLeod", 1, null, new DateTime(2026, 3, 11, 15, 16, 10, 223, DateTimeKind.Utc).AddTicks(3190), null, "1.0" }
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_field_mapping_templates_customer_id",
                table: "field_mapping_templates",
                column: "customer_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_field_mappings_template_version_id",
                table: "field_mappings",
                column: "template_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_mappings_template_version_id_execution_order",
                table: "field_mappings",
                columns: new[] { "template_version_id", "execution_order" });

            migrationBuilder.CreateIndex(
                name: "IX_lookup_tables_partner_id",
                table: "lookup_tables",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "IX_lookup_tables_tms_system_id",
                table: "lookup_tables",
                column: "tms_system_id");

            migrationBuilder.CreateIndex(
                name: "IX_lookup_tables_tms_system_id_field_name",
                table: "lookup_tables",
                columns: new[] { "tms_system_id", "field_name" });

            migrationBuilder.CreateIndex(
                name: "IX_partners_name",
                table: "partners",
                column: "name");

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

            migrationBuilder.CreateIndex(
                name: "IX_template_versions_template_id",
                table: "template_versions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_templates_name",
                table: "templates",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_tms_systems_is_active",
                table: "tms_systems",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_tms_systems_name",
                table: "tms_systems",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transformation_logs_expires_at",
                table: "transformation_logs",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_transformation_logs_status",
                table: "transformation_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_transformation_logs_template_id",
                table: "transformation_logs",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_transformation_logs_timestamp",
                table: "transformation_logs",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "field_mapping_templates");

            migrationBuilder.DropTable(
                name: "field_mappings");

            migrationBuilder.DropTable(
                name: "lookup_tables");

            migrationBuilder.DropTable(
                name: "template_assignments");

            migrationBuilder.DropTable(
                name: "transformation_logs");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "tms_systems");

            migrationBuilder.DropTable(
                name: "partners");

            migrationBuilder.DropTable(
                name: "template_versions");

            migrationBuilder.DropTable(
                name: "templates");
        }
    }
}
