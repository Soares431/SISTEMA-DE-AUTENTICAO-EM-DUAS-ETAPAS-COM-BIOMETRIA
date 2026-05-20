using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCPF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Pessoa_cpf",
                table: "Pessoa",
                column: "cpf",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pessoa_cpf",
                table: "Pessoa");
        }
    }
}
