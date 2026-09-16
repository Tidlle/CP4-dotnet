using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameCatalog.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_estudio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    s_nome = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    s_pais = table.Column<string>(type: "NVARCHAR2(60)", maxLength: 60, nullable: false),
                    i_ano_fundacao = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    b_ativo = table.Column<bool>(type: "BOOLEAN", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_estudio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_plataforma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    s_nome = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_plataforma", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_jogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    s_titulo = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    i_genero = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    n_preco = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    d_lancamento = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    n_nota = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    i_estudio_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_jogo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_jogo_tb_estudio_i_estudio_id",
                        column: x => x.i_estudio_id,
                        principalTable: "tb_estudio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_jogo_plataforma",
                columns: table => new
                {
                    JogosId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    PlataformasId = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_jogo_plataforma", x => new { x.JogosId, x.PlataformasId });
                    table.ForeignKey(
                        name: "FK_tb_jogo_plataforma_tb_jogo_JogosId",
                        column: x => x.JogosId,
                        principalTable: "tb_jogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_jogo_plataforma_tb_plataforma_PlataformasId",
                        column: x => x.PlataformasId,
                        principalTable: "tb_plataforma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_ESTUDIO_NOME",
                table: "tb_estudio",
                column: "s_nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_ESTUDIO_PAIS_ANO",
                table: "tb_estudio",
                columns: new[] { "s_pais", "i_ano_fundacao" });

            migrationBuilder.CreateIndex(
                name: "IDX_JOGO_ESTUDIO_DATA",
                table: "tb_jogo",
                columns: new[] { "i_estudio_id", "d_lancamento" });

            migrationBuilder.CreateIndex(
                name: "IDX_JOGO_GENERO_PRECO",
                table: "tb_jogo",
                columns: new[] { "i_genero", "n_preco" });

            migrationBuilder.CreateIndex(
                name: "IDX_JOGO_TITULO",
                table: "tb_jogo",
                column: "s_titulo");

            migrationBuilder.CreateIndex(
                name: "IX_tb_jogo_plataforma_PlataformasId",
                table: "tb_jogo_plataforma",
                column: "PlataformasId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_jogo_plataforma");

            migrationBuilder.DropTable(
                name: "tb_jogo");

            migrationBuilder.DropTable(
                name: "tb_plataforma");

            migrationBuilder.DropTable(
                name: "tb_estudio");
        }
    }
}
