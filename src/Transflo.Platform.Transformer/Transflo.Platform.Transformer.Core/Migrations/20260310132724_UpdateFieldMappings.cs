using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFieldMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: "tms-mcleod-001",
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 10, 13, 27, 23, 882, DateTimeKind.Utc).AddTicks(3086), new DateTime(2026, 3, 10, 13, 27, 23, 882, DateTimeKind.Utc).AddTicks(3086) });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: "tms-truckmate-001",
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 10, 13, 27, 23, 882, DateTimeKind.Utc).AddTicks(3086), new DateTime(2026, 3, 10, 13, 27, 23, 882, DateTimeKind.Utc).AddTicks(3086) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: "tms-mcleod-001",
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 9, 21, 9, 34, 454, DateTimeKind.Utc).AddTicks(8648), new DateTime(2026, 3, 9, 21, 9, 34, 454, DateTimeKind.Utc).AddTicks(8648) });

            migrationBuilder.UpdateData(
                table: "tms_systems",
                keyColumn: "id",
                keyValue: "tms-truckmate-001",
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 3, 9, 21, 9, 34, 454, DateTimeKind.Utc).AddTicks(8648), new DateTime(2026, 3, 9, 21, 9, 34, 454, DateTimeKind.Utc).AddTicks(8648) });
        }
    }
}
