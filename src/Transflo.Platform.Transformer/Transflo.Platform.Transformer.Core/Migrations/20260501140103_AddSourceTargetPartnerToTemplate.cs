using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceTargetPartnerToTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_partner_id",
                table: "templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_partner_id",
                table: "templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_templates_source_partner_id",
                table: "templates",
                column: "source_partner_id");

            migrationBuilder.CreateIndex(
                name: "IX_templates_target_partner_id",
                table: "templates",
                column: "target_partner_id");

            migrationBuilder.AddForeignKey(
                name: "FK_templates_partners_source_partner_id",
                table: "templates",
                column: "source_partner_id",
                principalTable: "partners",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_templates_partners_target_partner_id",
                table: "templates",
                column: "target_partner_id",
                principalTable: "partners",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_templates_partners_source_partner_id",
                table: "templates");

            migrationBuilder.DropForeignKey(
                name: "FK_templates_partners_target_partner_id",
                table: "templates");

            migrationBuilder.DropIndex(
                name: "IX_templates_source_partner_id",
                table: "templates");

            migrationBuilder.DropIndex(
                name: "IX_templates_target_partner_id",
                table: "templates");

            migrationBuilder.DropColumn(
                name: "source_partner_id",
                table: "templates");

            migrationBuilder.DropColumn(
                name: "target_partner_id",
                table: "templates");
        }
    }
}
