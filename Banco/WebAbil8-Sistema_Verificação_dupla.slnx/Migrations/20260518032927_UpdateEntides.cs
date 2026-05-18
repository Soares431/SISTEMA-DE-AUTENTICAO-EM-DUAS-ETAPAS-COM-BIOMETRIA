using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pessoa",
                table: "tentativaAcesso");

            migrationBuilder.CreateIndex(
                name: "IX_tentativaAcesso_pessoaId",
                table: "tentativaAcesso",
                column: "pessoaId");

            migrationBuilder.AddForeignKey(
                name: "FK_tentativaAcesso_Pessoa_pessoaId",
                table: "tentativaAcesso",
                column: "pessoaId",
                principalTable: "Pessoa",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tentativaAcesso_Pessoa_pessoaId",
                table: "tentativaAcesso");

            migrationBuilder.DropIndex(
                name: "IX_tentativaAcesso_pessoaId",
                table: "tentativaAcesso");

            migrationBuilder.AddColumn<long>(
                name: "Pessoa",
                table: "tentativaAcesso",
                type: "INTEGER",
                nullable: true);
        }
    }
}
