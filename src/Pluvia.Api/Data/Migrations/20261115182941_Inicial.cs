using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pluvia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fontes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fontes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ocorrencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FonteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Lng = table.Column<double>(type: "REAL", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 280, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AbertaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EncerradaEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Fontes_FonteId",
                        column: x => x.FonteId,
                        principalTable: "Fontes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sensores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FonteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Lng = table.Column<double>(type: "REAL", nullable: false),
                    LimiteCm = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sensores_Fontes_FonteId",
                        column: x => x.FonteId,
                        principalTable: "Fontes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Lng = table.Column<double>(type: "REAL", nullable: false),
                    RaioMetros = table.Column<int>(type: "INTEGER", nullable: false),
                    SilenciadaAte = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RiscoAtual = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Lng = table.Column<double>(type: "REAL", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 280, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FotoNome = table.Column<string>(type: "TEXT", nullable: true),
                    Avaliacao_Classificacao = table.Column<int>(type: "INTEGER", nullable: true),
                    Avaliacao_Confianca = table.Column<double>(type: "REAL", nullable: true),
                    Avaliacao_Modelo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Prioridade = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublicadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relatos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Veiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Apelido = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    RaioMetros = table.Column<int>(type: "INTEGER", nullable: false),
                    AvisosAtivos = table.Column<bool>(type: "INTEGER", nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: true),
                    Lng = table.Column<double>(type: "REAL", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veiculos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    OcorrenciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Mensagem = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alertas_Ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "Ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Historicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OcorrenciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FonteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Em = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Historicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Historicos_Fontes_FonteId",
                        column: x => x.FonteId,
                        principalTable: "Fontes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Historicos_Ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "Ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leituras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SensorId = table.Column<int>(type: "INTEGER", nullable: false),
                    NivelCm = table.Column<int>(type: "INTEGER", nullable: false),
                    RegistradaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leituras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leituras_Sensores_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_OcorrenciaId",
                table: "Alertas",
                column: "OcorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_UsuarioId_OcorrenciaId_Tipo",
                table: "Alertas",
                columns: new[] { "UsuarioId", "OcorrenciaId", "Tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_UsuarioId",
                table: "Areas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Fontes_Nome",
                table: "Fontes",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Historicos_FonteId",
                table: "Historicos",
                column: "FonteId");

            migrationBuilder.CreateIndex(
                name: "IX_Historicos_OcorrenciaId_Em",
                table: "Historicos",
                columns: new[] { "OcorrenciaId", "Em" });

            migrationBuilder.CreateIndex(
                name: "IX_Leituras_SensorId_RegistradaEm",
                table: "Leituras",
                columns: new[] { "SensorId", "RegistradaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_FonteId",
                table: "Ocorrencias",
                column: "FonteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_Status_Lat_Lng",
                table: "Ocorrencias",
                columns: new[] { "Status", "Lat", "Lng" });

            migrationBuilder.CreateIndex(
                name: "IX_Relatos_Status_Lat_Lng",
                table: "Relatos",
                columns: new[] { "Status", "Lat", "Lng" });

            migrationBuilder.CreateIndex(
                name: "IX_Relatos_UsuarioId_CriadoEm",
                table: "Relatos",
                columns: new[] { "UsuarioId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Sensores_Codigo",
                table: "Sensores",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sensores_FonteId",
                table: "Sensores",
                column: "FonteId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_UsuarioId",
                table: "Veiculos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alertas");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "Historicos");

            migrationBuilder.DropTable(
                name: "Leituras");

            migrationBuilder.DropTable(
                name: "Relatos");

            migrationBuilder.DropTable(
                name: "Veiculos");

            migrationBuilder.DropTable(
                name: "Ocorrencias");

            migrationBuilder.DropTable(
                name: "Sensores");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Fontes");
        }
    }
}
