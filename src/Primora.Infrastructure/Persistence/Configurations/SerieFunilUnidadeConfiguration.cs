using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class SerieFunilUnidadeConfiguration : IEntityTypeConfiguration<SerieFunilUnidade>
{
    public void Configure(EntityTypeBuilder<SerieFunilUnidade> builder)
    {
        builder.ToTable("serie_funil_unidade", table =>
        {
            table.HasCheckConstraint("ck_funil_posicao", "posicao BETWEEN 1 AND 30");
            table.HasCheckConstraint("ck_funil_valor", "valor >= 0 AND valor <= 1");
        });
        builder.HasKey(x => new { x.VersaoId, x.UnidadeId, x.Posicao }).HasName("pk_serie_funil_unidade");
        builder.Property(x => x.VersaoId).HasColumnName("versao_id").ValueGeneratedNever();
        builder.Property(x => x.UnidadeId).HasColumnName("unidade_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.Posicao).HasColumnName("posicao");
        builder.Property(x => x.Valor).HasColumnName("valor").HasColumnType("numeric");
        builder.HasOne(x => x.Unidade).WithMany(x => x.SerieFunil)
            .HasForeignKey(x => new { x.VersaoId, x.UnidadeId }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_funil_unidades");
    }
}
