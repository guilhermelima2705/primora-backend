using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class SerieEngajamentoUnidadeConfiguration : IEntityTypeConfiguration<SerieEngajamentoUnidade>
{
    public void Configure(EntityTypeBuilder<SerieEngajamentoUnidade> builder)
    {
        builder.ToTable("serie_engajamento_unidade", table =>
        {
            table.HasCheckConstraint("ck_engajamento_posicao", "posicao BETWEEN 1 AND 30");
            table.HasCheckConstraint("ck_engajamento_valor", "valor >= 0");
        });
        builder.HasKey(x => new { x.VersaoId, x.UnidadeId, x.Posicao }).HasName("pk_serie_engajamento_unidade");
        builder.Property(x => x.VersaoId).HasColumnName("versao_id").ValueGeneratedNever();
        builder.Property(x => x.UnidadeId).HasColumnName("unidade_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.Posicao).HasColumnName("posicao");
        builder.Property(x => x.Valor).HasColumnName("valor");
        builder.HasOne(x => x.Unidade).WithMany(x => x.SerieEngajamento)
            .HasForeignKey(x => new { x.VersaoId, x.UnidadeId }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_engajamento_unidades");
    }
}
