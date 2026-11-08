using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence.Configurations;

public sealed class VersaoDatasetConfiguration : IEntityTypeConfiguration<VersaoDataset>
{
    public void Configure(EntityTypeBuilder<VersaoDataset> builder)
    {
        builder.ToTable("versoes_dataset", table =>
        {
            table.HasCheckConstraint("ck_versoes_hash", "hash_sha256 ~ '^[0-9a-f]{64}$'");
            table.HasCheckConstraint("ck_versoes_origem", "length(btrim(nome_origem)) > 0 AND length(btrim(versao_importador)) > 0");
        });
        builder.HasKey(x => x.Id).HasName("pk_versoes_dataset");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.GeradoEm).HasColumnName("gerado_em").HasColumnType("date");
        builder.Property(x => x.ImportadoEm).HasColumnName("importado_em").HasColumnType("timestamp with time zone");
        builder.Property(x => x.HashSha256).HasColumnName("hash_sha256").HasColumnType("text").IsRequired();
        builder.Property(x => x.NomeOrigem).HasColumnName("nome_origem").HasColumnType("text").IsRequired();
        builder.Property(x => x.VersaoImportador).HasColumnName("versao_importador").HasColumnType("text").IsRequired();
        builder.HasIndex(x => x.HashSha256).IsUnique().HasDatabaseName("ux_versoes_hash");
        builder.HasIndex(x => x.GeradoEm).HasDatabaseName("ix_versoes_gerado_em");
    }
}
