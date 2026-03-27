using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseVersionToTemplateVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "base_version",
                table: "template_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 26, 20, 25, 20, 639, DateTimeKind.Utc).AddTicks(2069), new DateTime(2026, 3, 26, 20, 25, 20, 639, DateTimeKind.Utc).AddTicks(2069) });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 26, 20, 25, 20, 639, DateTimeKind.Utc).AddTicks(2069), new DateTime(2026, 3, 26, 20, 25, 20, 639, DateTimeKind.Utc).AddTicks(2069) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "base_version",
                table: "template_versions");

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 24, 19, 48, 3, 649, DateTimeKind.Utc).AddTicks(6880), new DateTime(2026, 3, 24, 19, 48, 3, 649, DateTimeKind.Utc).AddTicks(6880) });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 24, 19, 48, 3, 649, DateTimeKind.Utc).AddTicks(6880), new DateTime(2026, 3, 24, 19, 48, 3, 649, DateTimeKind.Utc).AddTicks(6880) });
        }
    }
}
