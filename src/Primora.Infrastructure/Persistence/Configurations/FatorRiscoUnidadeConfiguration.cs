using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class FatorRiscoUnidadeConfiguration : IEntityTypeConfiguration<FatorRiscoUnidade>
{
    public void Configure(EntityTypeBuilder<FatorRiscoUnidade> builder)
    {
        builder.ToTable("fatores_risco_unidade", table =>
        {
            table.HasCheckConstraint("ck_fatores_ordem", "ordem_origem > 0");
            table.HasCheckConstraint("ck_fatores_descricao", "length(btrim(descricao)) > 0");
        });
        builder.HasKey(x => new { x.VersaoId, x.UnidadeId, x.OrdemOrigem }).HasName("pk_fatores_risco_unidade");
        builder.Property(x => x.VersaoId).HasColumnName("versao_id").ValueGeneratedNever();
        builder.Property(x => x.UnidadeId).HasColumnName("unidade_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.OrdemOrigem).HasColumnName("ordem_origem");
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasColumnType("text").IsRequired();
        builder.HasOne(x => x.Unidade).WithMany(x => x.FatoresRisco)
            .HasForeignKey(x => new { x.VersaoId, x.UnidadeId }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fatores_unidades");
    }
}
