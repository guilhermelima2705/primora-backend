using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class IndicadoresUnidadeConfiguration : IEntityTypeConfiguration<IndicadoresUnidade>
{
    public void Configure(EntityTypeBuilder<IndicadoresUnidade> builder)
    {
        builder.ToTable("indicadores_unidade", table =>
        {
            table.HasCheckConstraint("ck_indicadores_nao_negativos", "carteira_total_clientes >= 0 AND receita_carteira_90d >= 0 AND eventos_engajamento_7d >= 0 AND usuarios_ativos_7d >= 0 AND funil_gerada_90d >= 0 AND funil_vista_90d >= 0 AND funil_sucesso_90d >= 0 AND funil_falha_90d >= 0 AND saudavel >= 0 AND atencao >= 0 AND risco >= 0 AND critico >= 0 AND risco_absoluto >= 0");
            table.HasCheckConstraint("ck_indicadores_distribuicao", "saudavel::bigint + atencao + risco + critico = carteira_total_clientes");
            table.HasCheckConstraint("ck_indicadores_risco", "risco::bigint + critico = risco_absoluto");
        });
        builder.HasKey(x => new { x.VersaoId, x.UnidadeId }).HasName("pk_indicadores_unidade");
        builder.Property(x => x.VersaoId).HasColumnName("versao_id").ValueGeneratedNever();
        builder.Property(x => x.UnidadeId).HasColumnName("unidade_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.CarteiraTotalClientes).HasColumnName("carteira_total_clientes");
        builder.Property(x => x.ReceitaCarteira90d).HasColumnName("receita_carteira_90d").HasPrecision(18, 2);
        builder.Property(x => x.EventosEngajamento7d).HasColumnName("eventos_engajamento_7d");
        builder.Property(x => x.UsuariosAtivos7d).HasColumnName("usuarios_ativos_7d");
        builder.Property(x => x.FunilGerada90d).HasColumnName("funil_gerada_90d");
        builder.Property(x => x.FunilVista90d).HasColumnName("funil_vista_90d");
        builder.Property(x => x.FunilSucesso90d).HasColumnName("funil_sucesso_90d");
        builder.Property(x => x.FunilFalha90d).HasColumnName("funil_falha_90d");
        builder.Property(x => x.Saudavel).HasColumnName("saudavel");
        builder.Property(x => x.Atencao).HasColumnName("atencao");
        builder.Property(x => x.Risco).HasColumnName("risco");
        builder.Property(x => x.Critico).HasColumnName("critico");
        builder.Property(x => x.RiscoAbsoluto).HasColumnName("risco_absoluto");
        builder.HasOne(x => x.Unidade).WithOne(x => x.Indicadores)
            .HasForeignKey<IndicadoresUnidade>(x => new { x.VersaoId, x.UnidadeId }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_indicadores_unidades");
    }
}
