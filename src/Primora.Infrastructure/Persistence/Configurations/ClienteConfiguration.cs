using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes", table =>
        {
            table.HasCheckConstraint("ck_clientes_textos", "length(btrim(id)) > 0 AND length(btrim(nome)) > 0");
            table.HasCheckConstraint("ck_clientes_ordem", "ordem_origem > 0");
        });
        builder.HasKey(x => new { x.VersaoId, x.UnidadeId, x.Id }).HasName("pk_clientes");
        builder.Property(x => x.VersaoId).HasColumnName("versao_id").ValueGeneratedNever();
        builder.Property(x => x.UnidadeId).HasColumnName("unidade_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("text").IsRequired();
        builder.Property(x => x.Nome).HasColumnName("nome").HasColumnType("text").IsRequired();
        builder.Property(x => x.OrdemOrigem).HasColumnName("ordem_origem");
        builder.HasOne(x => x.Unidade).WithMany(x => x.Clientes)
            .HasForeignKey(x => new { x.VersaoId, x.UnidadeId }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_clientes_unidades");
        builder.HasIndex(x => new { x.VersaoId, x.OrdemOrigem }).IsUnique().HasDatabaseName("ux_clientes_ordem");
    }
}
