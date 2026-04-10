using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transflo.Platform.Transformer.Core.Migrations
{
    /// <inheritdoc />
    public partial class removeexecutionorderfieldfromfieldmapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_field_mappings_template_version_id_execution_order",
                table: "field_mappings");

            migrationBuilder.DropColumn(
                name: "execution_order",
                table: "field_mappings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "execution_order",
                table: "field_mappings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_field_mappings_template_version_id_execution_order",
                table: "field_mappings",
                columns: new[] { "template_version_id", "execution_order" });
        }
    }
}
