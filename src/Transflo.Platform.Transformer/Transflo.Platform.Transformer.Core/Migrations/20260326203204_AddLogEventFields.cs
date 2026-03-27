using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddLogEventFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "transformation_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "message_summary",
                table: "transformation_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transformation_logs_correlation_id",
                table: "transformation_logs",
                column: "correlation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transformation_logs_correlation_id",
                table: "transformation_logs");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "transformation_logs");

            migrationBuilder.DropColumn(
                name: "message_summary",
                table: "transformation_logs");
        }
    }
}
