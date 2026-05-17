using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCamera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ativa",
                table: "camera",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ativa",
                table: "camera");
        }
    }
}
