using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pessoa",
                table: "senhaDisponivel");

            migrationBuilder.CreateIndex(
                name: "IX_senhaDisponivel_pessoaId",
                table: "senhaDisponivel",
                column: "pessoaId");

            migrationBuilder.AddForeignKey(
                name: "FK_senhaDisponivel_Pessoa_pessoaId",
                table: "senhaDisponivel",
                column: "pessoaId",
                principalTable: "Pessoa",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_senhaDisponivel_Pessoa_pessoaId",
                table: "senhaDisponivel");

            migrationBuilder.DropIndex(
                name: "IX_senhaDisponivel_pessoaId",
                table: "senhaDisponivel");

            migrationBuilder.AddColumn<long>(
                name: "Pessoa",
                table: "senhaDisponivel",
                type: "INTEGER",
                nullable: true);
        }
    }
}
