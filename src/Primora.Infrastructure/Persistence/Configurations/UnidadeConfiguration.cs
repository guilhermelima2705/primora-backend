using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class UnidadeConfiguration : IEntityTypeConfiguration<Unidade>
{
    public void Configure(EntityTypeBuilder<Unidade> builder)
    {
        builder.ToTable("unidades", table =>
        {
            table.HasCheckConstraint("ck_unidades_textos", "length(btrim(id)) > 0 AND length(btrim(nome)) > 0 AND length(btrim(porte)) > 0");
            table.HasCheckConstraint("ck_unidades_ordem", "ordem_origem > 0");
        });
        builder.HasKey(x => new { x.VersaoId, x.Id }).HasName("pk_unidades");
        builder.Property(x => x.VersaoId).HasColumnName("versao_id").ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("text").IsRequired();
        builder.Property(x => x.Nome).HasColumnName("nome").HasColumnType("text").IsRequired();
        builder.Property(x => x.Porte).HasColumnName("porte").HasColumnType("text").IsRequired();
        builder.Property(x => x.OrdemOrigem).HasColumnName("ordem_origem");
        builder.HasOne(x => x.Versao).WithMany(x => x.Unidades)
            .HasForeignKey(x => x.VersaoId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_unidades_versoes");
        builder.HasIndex(x => new { x.VersaoId, x.OrdemOrigem }).IsUnique().HasDatabaseName("ux_unidades_ordem");
    }
}
