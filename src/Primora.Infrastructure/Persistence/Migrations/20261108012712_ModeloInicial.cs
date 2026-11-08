using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Primora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModeloInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "versoes_dataset",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gerado_em = table.Column<DateOnly>(type: "date", nullable: false),
                    importado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hash_sha256 = table.Column<string>(type: "text", nullable: false),
                    nome_origem = table.Column<string>(type: "text", nullable: false),
                    versao_importador = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_versoes_dataset", x => x.id);
                    table.CheckConstraint("ck_versoes_hash", "hash_sha256 ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_versoes_origem", "length(btrim(nome_origem)) > 0 AND length(btrim(versao_importador)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "unidades",
                schema: "public",
                columns: table => new
                {
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<string>(type: "text", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    porte = table.Column<string>(type: "text", nullable: false),
                    ordem_origem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unidades", x => new { x.versao_id, x.id });
                    table.CheckConstraint("ck_unidades_ordem", "ordem_origem > 0");
                    table.CheckConstraint("ck_unidades_textos", "length(btrim(id)) > 0 AND length(btrim(nome)) > 0 AND length(btrim(porte)) > 0");
                    table.ForeignKey(
                        name: "fk_unidades_versoes",
                        column: x => x.versao_id,
                        principalSchema: "public",
                        principalTable: "versoes_dataset",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "public",
                columns: table => new
                {
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidade_id = table.Column<string>(type: "text", nullable: false),
                    id = table.Column<string>(type: "text", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    ordem_origem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => new { x.versao_id, x.unidade_id, x.id });
                    table.CheckConstraint("ck_clientes_ordem", "ordem_origem > 0");
                    table.CheckConstraint("ck_clientes_textos", "length(btrim(id)) > 0 AND length(btrim(nome)) > 0");
                    table.ForeignKey(
                        name: "fk_clientes_unidades",
                        columns: x => new { x.versao_id, x.unidade_id },
                        principalSchema: "public",
                        principalTable: "unidades",
                        principalColumns: new[] { "versao_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fatores_risco_unidade",
                schema: "public",
                columns: table => new
                {
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidade_id = table.Column<string>(type: "text", nullable: false),
                    ordem_origem = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fatores_risco_unidade", x => new { x.versao_id, x.unidade_id, x.ordem_origem });
                    table.CheckConstraint("ck_fatores_descricao", "length(btrim(descricao)) > 0");
                    table.CheckConstraint("ck_fatores_ordem", "ordem_origem > 0");
                    table.ForeignKey(
                        name: "fk_fatores_unidades",
                        columns: x => new { x.versao_id, x.unidade_id },
                        principalSchema: "public",
                        principalTable: "unidades",
                        principalColumns: new[] { "versao_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "indicadores_unidade",
                schema: "public",
                columns: table => new
                {
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidade_id = table.Column<string>(type: "text", nullable: false),
                    carteira_total_clientes = table.Column<int>(type: "integer", nullable: false),
                    receita_carteira_90d = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    eventos_engajamento_7d = table.Column<int>(type: "integer", nullable: false),
                    usuarios_ativos_7d = table.Column<int>(type: "integer", nullable: false),
                    funil_gerada_90d = table.Column<int>(type: "integer", nullable: false),
                    funil_vista_90d = table.Column<int>(type: "integer", nullable: false),
                    funil_sucesso_90d = table.Column<int>(type: "integer", nullable: false),
                    funil_falha_90d = table.Column<int>(type: "integer", nullable: false),
                    saudavel = table.Column<int>(type: "integer", nullable: false),
                    atencao = table.Column<int>(type: "integer", nullable: false),
                    risco = table.Column<int>(type: "integer", nullable: false),
                    critico = table.Column<int>(type: "integer", nullable: false),
                    risco_absoluto = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_indicadores_unidade", x => new { x.versao_id, x.unidade_id });
                    table.CheckConstraint("ck_indicadores_distribuicao", "saudavel::bigint + atencao + risco + critico = carteira_total_clientes");
                    table.CheckConstraint("ck_indicadores_nao_negativos", "carteira_total_clientes >= 0 AND receita_carteira_90d >= 0 AND eventos_engajamento_7d >= 0 AND usuarios_ativos_7d >= 0 AND funil_gerada_90d >= 0 AND funil_vista_90d >= 0 AND funil_sucesso_90d >= 0 AND funil_falha_90d >= 0 AND saudavel >= 0 AND atencao >= 0 AND risco >= 0 AND critico >= 0 AND risco_absoluto >= 0");
                    table.CheckConstraint("ck_indicadores_risco", "risco::bigint + critico = risco_absoluto");
                    table.ForeignKey(
                        name: "fk_indicadores_unidades",
                        columns: x => new { x.versao_id, x.unidade_id },
                        principalSchema: "public",
                        principalTable: "unidades",
                        principalColumns: new[] { "versao_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "serie_engajamento_unidade",
                schema: "public",
                columns: table => new
                {
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidade_id = table.Column<string>(type: "text", nullable: false),
                    posicao = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serie_engajamento_unidade", x => new { x.versao_id, x.unidade_id, x.posicao });
                    table.CheckConstraint("ck_engajamento_posicao", "posicao BETWEEN 1 AND 30");
                    table.CheckConstraint("ck_engajamento_valor", "valor >= 0");
                    table.ForeignKey(
                        name: "fk_engajamento_unidades",
                        columns: x => new { x.versao_id, x.unidade_id },
                        principalSchema: "public",
                        principalTable: "unidades",
                        principalColumns: new[] { "versao_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "serie_funil_unidade",
                schema: "public",
                columns: table => new
                {
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidade_id = table.Column<string>(type: "text", nullable: false),
                    posicao = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serie_funil_unidade", x => new { x.versao_id, x.unidade_id, x.posicao });
                    table.CheckConstraint("ck_funil_posicao", "posicao BETWEEN 1 AND 30");
                    table.CheckConstraint("ck_funil_valor", "valor >= 0 AND valor <= 1");
                    table.ForeignKey(
                        name: "fk_funil_unidades",
                        columns: x => new { x.versao_id, x.unidade_id },
                        principalSchema: "public",
                        principalTable: "unidades",
                        principalColumns: new[] { "versao_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventos_cliente",
                schema: "public",
                columns: table => new
                {
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidade_id = table.Column<string>(type: "text", nullable: false),
                    cliente_id = table.Column<string>(type: "text", nullable: false),
                    ordem_origem = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    titulo = table.Column<string>(type: "text", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_cliente", x => new { x.versao_id, x.unidade_id, x.cliente_id, x.ordem_origem });
                    table.CheckConstraint("ck_eventos_ordem", "ordem_origem > 0");
                    table.CheckConstraint("ck_eventos_textos", "length(btrim(tipo)) > 0 AND length(btrim(titulo)) > 0");
                    table.CheckConstraint("ck_eventos_valor", "valor IS NULL OR valor >= 0");
                    table.ForeignKey(
                        name: "fk_eventos_clientes",
                        columns: x => new { x.versao_id, x.unidade_id, x.cliente_id },
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumns: new[] { "versao_id", "unidade_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "historico_saude_cliente",
                schema: "public",
                columns: table => new
                {
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidade_id = table.Column<string>(type: "text", nullable: false),
                    cliente_id = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historico_saude_cliente", x => new { x.versao_id, x.unidade_id, x.cliente_id, x.data });
                    table.CheckConstraint("ck_historico_score", "score BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "fk_historico_clientes",
                        columns: x => new { x.versao_id, x.unidade_id, x.cliente_id },
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumns: new[] { "versao_id", "unidade_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_clientes_ordem",
                schema: "public",
                table: "clientes",
                columns: new[] { "versao_id", "ordem_origem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_eventos_cliente_data",
                schema: "public",
                table: "eventos_cliente",
                columns: new[] { "versao_id", "unidade_id", "cliente_id", "data" });

            migrationBuilder.CreateIndex(
                name: "ux_unidades_ordem",
                schema: "public",
                table: "unidades",
                columns: new[] { "versao_id", "ordem_origem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_versoes_gerado_em",
                schema: "public",
                table: "versoes_dataset",
                column: "gerado_em");

            migrationBuilder.CreateIndex(
                name: "ux_versoes_hash",
                schema: "public",
                table: "versoes_dataset",
                column: "hash_sha256",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_cliente",
                schema: "public");

            migrationBuilder.DropTable(
                name: "fatores_risco_unidade",
                schema: "public");

            migrationBuilder.DropTable(
                name: "historico_saude_cliente",
                schema: "public");

            migrationBuilder.DropTable(
                name: "indicadores_unidade",
                schema: "public");

            migrationBuilder.DropTable(
                name: "serie_engajamento_unidade",
                schema: "public");

            migrationBuilder.DropTable(
                name: "serie_funil_unidade",
                schema: "public");

            migrationBuilder.DropTable(
                name: "clientes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "unidades",
                schema: "public");

            migrationBuilder.DropTable(
                name: "versoes_dataset",
                schema: "public");
        }
    }
}
