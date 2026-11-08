using Microsoft.EntityFrameworkCore;
using Primora.Domain.Entities;

namespace Primora.Infrastructure.Persistence;

public sealed class PrimoraDbContext(DbContextOptions<PrimoraDbContext> options) : DbContext(options)
{
    public DbSet<VersaoDataset> VersoesDataset => Set<VersaoDataset>();
    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<IndicadoresUnidade> IndicadoresUnidades => Set<IndicadoresUnidade>();
    public DbSet<SerieEngajamentoUnidade> SeriesEngajamento => Set<SerieEngajamentoUnidade>();
    public DbSet<SerieFunilUnidade> SeriesFunil => Set<SerieFunilUnidade>();
    public DbSet<FatorRiscoUnidade> FatoresRisco => Set<FatorRiscoUnidade>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<HistoricoSaudeCliente> HistoricosSaude => Set<HistoricoSaudeCliente>();
    public DbSet<EventoCliente> EventosClientes => Set<EventoCliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrimoraDbContext).Assembly);
    }
}
