using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class EventoClienteConfiguration : IEntityTypeConfiguration<EventoCliente>
{
    public void Configure(EntityTypeBuilder<EventoCliente> builder)
    {
        builder.ToTable("eventos_cliente", table =>
        {
            table.HasCheckConstraint("ck_eventos_ordem", "ordem_origem > 0");
            table.HasCheckConstraint("ck_eventos_textos", "length(btrim(tipo)) > 0 AND length(btrim(titulo)) > 0");
            table.HasCheckConstraint("ck_eventos_valor", "valor IS NULL OR valor >= 0");
        });
        builder.HasKey(x => new { x.VersaoId, x.UnidadeId, x.ClienteId, x.OrdemOrigem }).HasName("pk_eventos_cliente");
        builder.Property(x => x.VersaoId).HasColumnName("versao_id").ValueGeneratedNever();
        builder.Property(x => x.UnidadeId).HasColumnName("unidade_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.ClienteId).HasColumnName("cliente_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.OrdemOrigem).HasColumnName("ordem_origem");
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("timestamp without time zone");
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasColumnType("text").IsRequired();
        builder.Property(x => x.Titulo).HasColumnName("titulo").HasColumnType("text").IsRequired();
        builder.Property(x => x.Valor).HasColumnName("valor").HasPrecision(18, 2);
        builder.HasOne(x => x.Cliente).WithMany(x => x.Eventos)
            .HasForeignKey(x => new { x.VersaoId, x.UnidadeId, x.ClienteId }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_eventos_clientes");
        builder.HasIndex(x => new { x.VersaoId, x.UnidadeId, x.ClienteId, x.Data }).HasDatabaseName("ix_eventos_cliente_data");
    }
}
