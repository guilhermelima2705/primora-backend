using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class HistoricoSaudeClienteConfiguration : IEntityTypeConfiguration<HistoricoSaudeCliente>
{
    public void Configure(EntityTypeBuilder<HistoricoSaudeCliente> builder)
    {
        builder.ToTable("historico_saude_cliente", table =>
        {
            table.HasCheckConstraint("ck_historico_score", "score BETWEEN 0 AND 100");
        });
        builder.HasKey(x => new { x.VersaoId, x.UnidadeId, x.ClienteId, x.Data }).HasName("pk_historico_saude_cliente");
        builder.Property(x => x.VersaoId).HasColumnName("versao_id").ValueGeneratedNever();
        builder.Property(x => x.UnidadeId).HasColumnName("unidade_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.ClienteId).HasColumnName("cliente_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("date");
        builder.Property(x => x.Score).HasColumnName("score");
        builder.HasOne(x => x.Cliente).WithMany(x => x.HistoricoSaude)
            .HasForeignKey(x => new { x.VersaoId, x.UnidadeId, x.ClienteId }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_historico_clientes");
    }
}
