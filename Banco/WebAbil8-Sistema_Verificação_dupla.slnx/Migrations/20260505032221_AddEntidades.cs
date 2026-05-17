using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Migrations
{
    /// <inheritdoc />
    public partial class AddEntidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "administrador",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    login = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    senhaHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    nomeCompleto = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    dataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administrador", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ambiente",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    dispositivoT50Id = table.Column<int>(type: "INTEGER", nullable: false),
                    tempoEsperaGravacaoSeg = table.Column<int>(type: "INTEGER", nullable: false),
                    dataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ambiente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuracao",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    retencaoGravacoesTentativasDias = table.Column<int>(type: "INTEGER", nullable: false),
                    retencaoLogsDias = table.Column<int>(type: "INTEGER", nullable: false),
                    tempoEsperaGravacaoSeg = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracao", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dispositivoT50",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    enderecoIP = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    porta = table.Column<int>(type: "INTEGER", nullable: false),
                    digitaisCadastradas = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispositivoT50", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Pessoa",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nome = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    cpf = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    cargo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    senhaHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    senhaClear = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    modoAcesso = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    biometriaCadastrada = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    templateBackup = table.Column<byte[]>(type: "BLOB", nullable: true),
                    status = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    dataUltimoAcesso = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    dataCadastro = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pessoa", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "senhaDisponivel",
                columns: table => new
                {
                    senha = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false),
                    emUso = table.Column<bool>(type: "INTEGER", nullable: false),
                    pessoaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Pessoa = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_senhaDisponivel", x => x.senha);
                });

            migrationBuilder.CreateTable(
                name: "logAdmin",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    adminId = table.Column<int>(type: "INTEGER", nullable: false),
                    acao = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    entidadeAfetada = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    entidadeId = table.Column<int>(type: "INTEGER", nullable: true),
                    dataHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dataExpiracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logAdmin", x => x.id);
                    table.ForeignKey(
                        name: "FK_logAdmin_administrador_adminId",
                        column: x => x.adminId,
                        principalTable: "administrador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "camera",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ambienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    urlRTSP = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    enderecoONVIF = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    tipo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_camera", x => x.id);
                    table.ForeignKey(
                        name: "FK_camera_ambiente_ambienteId",
                        column: x => x.ambienteId,
                        principalTable: "ambiente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tentativaAcesso",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pessoaId = table.Column<int>(type: "INTEGER", nullable: true),
                    ambienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    dataHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    acessoLiberado = table.Column<bool>(type: "INTEGER", nullable: false),
                    motivoNegacao = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    tipoVerificacao = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true),
                    gravacaoPath = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    dataExpiracao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Pessoa = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tentativaAcesso", x => x.id);
                    table.ForeignKey(
                        name: "FK_tentativaAcesso_ambiente_ambienteId",
                        column: x => x.ambienteId,
                        principalTable: "ambiente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ambiente_pessoa",
                columns: table => new
                {
                    ambienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    pessoaId = table.Column<long>(type: "INTEGER", nullable: false),
                    dataAdicionado = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ambiente_pessoa", x => new { x.ambienteId, x.pessoaId });
                    table.ForeignKey(
                        name: "FK_ambiente_pessoa_Pessoa_pessoaId",
                        column: x => x.pessoaId,
                        principalTable: "Pessoa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ambiente_pessoa_ambiente_ambienteId",
                        column: x => x.ambienteId,
                        principalTable: "ambiente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ambiente_pessoa_pessoaId",
                table: "ambiente_pessoa",
                column: "pessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_camera_ambienteId",
                table: "camera",
                column: "ambienteId");

            migrationBuilder.CreateIndex(
                name: "IX_logAdmin_adminId",
                table: "logAdmin",
                column: "adminId");

            migrationBuilder.CreateIndex(
                name: "IX_tentativaAcesso_ambienteId",
                table: "tentativaAcesso",
                column: "ambienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ambiente_pessoa");

            migrationBuilder.DropTable(
                name: "camera");

            migrationBuilder.DropTable(
                name: "configuracao");

            migrationBuilder.DropTable(
                name: "dispositivoT50");

            migrationBuilder.DropTable(
                name: "logAdmin");

            migrationBuilder.DropTable(
                name: "senhaDisponivel");

            migrationBuilder.DropTable(
                name: "tentativaAcesso");

            migrationBuilder.DropTable(
                name: "Pessoa");

            migrationBuilder.DropTable(
                name: "administrador");

            migrationBuilder.DropTable(
                name: "ambiente");
        }
    }
}
