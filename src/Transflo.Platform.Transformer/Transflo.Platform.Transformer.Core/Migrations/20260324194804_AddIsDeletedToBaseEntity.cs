using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tms_systems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "template_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "template_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "partners",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "field_mappings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "is_deleted", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 24, 19, 48, 3, 649, DateTimeKind.Utc).AddTicks(6880), false, new DateTime(2026, 3, 24, 19, 48, 3, 649, DateTimeKind.Utc).AddTicks(6880) });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "is_deleted", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 24, 19, 48, 3, 649, DateTimeKind.Utc).AddTicks(6880), false, new DateTime(2026, 3, 24, 19, 48, 3, 649, DateTimeKind.Utc).AddTicks(6880) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tms_systems");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "templates");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "template_versions");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "template_assignments");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "partners");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "field_mappings");

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
    }
}
