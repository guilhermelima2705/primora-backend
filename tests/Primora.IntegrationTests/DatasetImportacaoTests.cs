using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Primora.Application.Importacao;
using Primora.Domain.Entities;
using Primora.Infrastructure.Persistence;
using Primora.Tests.Fixtures;

namespace Primora.IntegrationTests;

[Trait("Category", "PostgreSQL")]
public sealed class DatasetImportacaoTests : IAsyncLifetime
{
    private PrimoraApiFactory rootFactory = null!;
    private WebApplicationFactory<Program> factory = null!;
    private AsyncServiceScope scope;
    private PrimoraDbContext database = null!;
    private IDatasetImportador importer = null!;
    private IDbContextTransaction? transaction;
    private readonly FailSecondSave failure = new();

    public async Task InitializeAsync()
    {
        rootFactory = new PrimoraApiFactory(realDatabase: true);
        factory = rootFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<PrimoraDbContext>(options => options.AddInterceptors(failure))));
        scope = factory.Services.CreateAsyncScope();
        database = scope.ServiceProvider.GetRequiredService<PrimoraDbContext>();
        importer = scope.ServiceProvider.GetRequiredService<IDatasetImportador>();
        transaction = await database.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
        }
        await scope.DisposeAsync();
        if (factory is not null) await factory.DisposeAsync();
        if (rootFactory is not null) await rootFactory.DisposeAsync();
    }

    [Fact]
    public async Task Initial_import_populates_all_nine_tables_with_correct_relationships()
    {
        var source = DatasetFixture.Create();
        var bytes = DatasetFixture.Bytes(source);
        var result = await importer.ImportarAsync(bytes, "fixture.json");
        Assert.True(result.NovaVersao);
        Assert.Equal(new ContagensDataset(1, 2, 2, 1, 2, 2, 60, 60, 4), await Counts(result.VersaoId));
        Assert.Equal(result.Contagens, await Counts(result.VersaoId));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), result.HashSha256);
        var client = await database.Clientes.AsNoTracking().Include(x => x.Unidade)
            .SingleAsync(x => x.VersaoId == result.VersaoId);
        Assert.Equal("unidade-a", client.UnidadeId);
        Assert.Equal(source["tenants"]![0]!["nome"]!.GetValue<string>(), client.Unidade.Nome);
        var events = await database.EventosClientes.AsNoTracking().Include(x => x.Cliente)
            .Where(x => x.VersaoId == result.VersaoId).ToListAsync();
        Assert.All(events, item => Assert.Equal(client.Id, item.Cliente.Id));
        Assert.All(events, item => Assert.Equal(client.UnidadeId, item.Cliente.UnidadeId));
    }

    [Fact]
    public async Task Reimport_is_a_noop_even_when_the_filename_changes()
    {
        var bytes = DatasetFixture.Bytes(DatasetFixture.Create());
        var first = await importer.ImportarAsync(bytes, "original.json");
        var before = await Counts(first.VersaoId);
        var timestamp = (await database.VersoesDataset.AsNoTracking().SingleAsync(x => x.Id == first.VersaoId)).ImportadoEm;
        var second = await importer.ImportarAsync(bytes, "renomeado.json");
        Assert.False(second.NovaVersao);
        Assert.Equal(first.VersaoId, second.VersaoId);
        Assert.Equal(before, await Counts(first.VersaoId));
        var version = await database.VersoesDataset.AsNoTracking().SingleAsync(x => x.Id == first.VersaoId);
        Assert.Equal(timestamp, version.ImportadoEm);
        Assert.Equal("original.json", version.NomeOrigem);
    }

    [Fact]
    public async Task New_valid_content_creates_another_version_even_with_the_same_reference_date()
    {
        var first = await importer.ImportarAsync(DatasetFixture.Bytes(DatasetFixture.Create()), "first.json");
        var second = await importer.ImportarAsync(DatasetFixture.Bytes(DatasetFixture.Create()), "second.json");
        Assert.True(first.NovaVersao);
        Assert.True(second.NovaVersao);
        Assert.NotEqual(first.VersaoId, second.VersaoId);
        Assert.NotEqual(first.HashSha256, second.HashSha256);
        Assert.Equal(first.Contagens, await Counts(second.VersaoId));
    }

    [Theory]
    [InlineData("invalid-tenant")]
    [InlineData("duplicate-unit")]
    [InlineData("duplicate-client")]
    [InlineData("tenant-name")]
    [InlineData("late-invalid-event")]
    public async Task Invalid_references_duplicates_and_late_errors_never_write_a_version(string scenario)
    {
        var count = await database.VersoesDataset.CountAsync();
        await Assert.ThrowsAsync<DatasetInvalidoException>(() => importer.ImportarAsync(
            DatasetFixture.Bytes(DatasetFixture.Invalid(scenario)), "invalid.json"));
        Assert.Equal(count, await database.VersoesDataset.CountAsync());
        Assert.False(database.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task Malformed_json_does_not_write_to_the_database()
    {
        var count = await database.VersoesDataset.CountAsync();
        await Assert.ThrowsAsync<DatasetInvalidoException>(() => importer.ImportarAsync("{"u8.ToArray(), "invalid.json"));
        Assert.Equal(count, await database.VersoesDataset.CountAsync());
    }

    [Fact]
    public async Task Failure_after_the_first_save_rolls_back_the_entire_import_and_preserves_outer_work()
    {
        var sentinel = new LeitorDataset().Preparar(DatasetFixture.Bytes(DatasetFixture.Create()), "sentinel.json").Versao;
        database.VersoesDataset.Add(sentinel);
        await database.SaveChangesAsync();
        var count = await database.VersoesDataset.CountAsync();
        failure.Enabled = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportarAsync(
            DatasetFixture.Bytes(DatasetFixture.Create()), "failure.json"));
        Assert.True(failure.FirstSaveCompleted);
        Assert.NotEqual(Guid.Empty, failure.AttemptedVersion);
        Assert.Equal(new ContagensDataset(0, 0, 0, 0, 0, 0, 0, 0, 0), await Counts(failure.AttemptedVersion));
        Assert.Equal(count, await database.VersoesDataset.CountAsync());
        Assert.True(await database.VersoesDataset.AnyAsync(x => x.Id == sentinel.Id));
        Assert.False(database.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task Owned_transaction_is_also_rolled_back_after_a_partial_save_failure()
    {
        await using var isolatedScope = factory.Services.CreateAsyncScope();
        var isolatedDatabase = isolatedScope.ServiceProvider.GetRequiredService<PrimoraDbContext>();
        var isolatedImporter = isolatedScope.ServiceProvider.GetRequiredService<IDatasetImportador>();
        Assert.Null(isolatedDatabase.Database.CurrentTransaction);
        failure.Enabled = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => isolatedImporter.ImportarAsync(
            DatasetFixture.Bytes(DatasetFixture.Create()), "owned-failure.json"));
        Assert.True(failure.FirstSaveCompleted);
        Assert.Null(isolatedDatabase.Database.CurrentTransaction);
        Assert.False(isolatedDatabase.ChangeTracker.HasChanges());
        Assert.Equal(new ContagensDataset(0, 0, 0, 0, 0, 0, 0, 0, 0), await Counts(failure.AttemptedVersion));
    }

    [Fact]
    public async Task Readback_preserves_money_decimal_precision_series_and_original_event_order()
    {
        var result = await importer.ImportarAsync(DatasetFixture.Bytes(DatasetFixture.Create()), "values.json");
        database.ChangeTracker.Clear();
        var indicators = await database.IndicadoresUnidades.Where(x => x.VersaoId == result.VersaoId).ToListAsync();
        Assert.All(indicators, item =>
        {
            Assert.Equal(10, item.CarteiraTotalClientes);
            Assert.Equal(12345.67m, item.ReceitaCarteira90d);
            Assert.Equal(123, item.EventosEngajamento7d);
            Assert.Equal(2, item.UsuariosAtivos7d);
            Assert.Equal(new[] { 10, 8, 4, 3 }, new[] { item.FunilGerada90d, item.FunilVista90d, item.FunilSucesso90d, item.FunilFalha90d });
            Assert.Equal(new[] { 3, 3, 2, 2 }, new[] { item.Saudavel, item.Atencao, item.Risco, item.Critico });
            Assert.Equal(4, item.RiscoAbsoluto);
        });
        var engagement = await database.SeriesEngajamento.Where(x => x.VersaoId == result.VersaoId && x.UnidadeId == "unidade-a")
            .OrderBy(x => x.Posicao).ToListAsync();
        Assert.Equal(Enumerable.Range(1, 30), engagement.Select(x => x.Valor));
        var funnel = await database.SeriesFunil.Where(x => x.VersaoId == result.VersaoId).ToListAsync();
        Assert.All(funnel, point => Assert.Equal(DatasetFixture.PreciseRatio, point.Valor));
        var events = await database.EventosClientes.Where(x => x.VersaoId == result.VersaoId).OrderBy(x => x.OrdemOrigem).ToListAsync();
        Assert.Equal(new[] { "Pedido de teste", "Nota no mesmo horário" }, events.Select(x => x.Titulo));
        Assert.Equal(98.76m, events[0].Valor);
        Assert.Null(events[1].Valor);
        Assert.All(events, item =>
        {
            Assert.Equal(DateTimeKind.Unspecified, item.Data.Kind);
            Assert.Equal(new DateTime(2026, 9, 1, 9, 12, 0), item.Data);
        });
        var scores = await database.HistoricosSaude.Where(x => x.VersaoId == result.VersaoId).OrderBy(x => x.Data).ToListAsync();
        Assert.Equal(new[] { 47, 52 }, scores.Select(x => x.Score));
        var factors = await database.FatoresRisco.Where(x => x.VersaoId == result.VersaoId && x.UnidadeId == "unidade-a")
            .OrderBy(x => x.OrdemOrigem).Select(x => x.Descricao).ToListAsync();
        Assert.Equal(new[] { "Fator de teste A", "Fator de teste B" }, factors);
        Assert.Equal(20, indicators.Sum(x => x.CarteiraTotalClientes));
        Assert.Single(await database.Clientes.Where(x => x.VersaoId == result.VersaoId).ToListAsync());
    }

    private async Task<ContagensDataset> Counts(Guid id) => new(
        await database.VersoesDataset.CountAsync(x => x.Id == id),
        await database.Unidades.CountAsync(x => x.VersaoId == id),
        await database.IndicadoresUnidades.CountAsync(x => x.VersaoId == id),
        await database.Clientes.CountAsync(x => x.VersaoId == id),
        await database.HistoricosSaude.CountAsync(x => x.VersaoId == id),
        await database.EventosClientes.CountAsync(x => x.VersaoId == id),
        await database.SeriesEngajamento.CountAsync(x => x.VersaoId == id),
        await database.SeriesFunil.CountAsync(x => x.VersaoId == id),
        await database.FatoresRisco.CountAsync(x => x.VersaoId == id));

    private sealed class FailSecondSave : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public bool FirstSaveCompleted { get; private set; }
        public Guid AttemptedVersion { get; private set; }
        private int saves;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled)
            {
                saves++;
                if (saves == 2) throw new InvalidOperationException("Falha simulada após a gravação da versão.");
                AttemptedVersion = eventData.Context!.ChangeTracker.Entries<VersaoDataset>()
                    .Single(x => x.State == EntityState.Added).Entity.Id;
            }
            return ValueTask.FromResult(result);
        }
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled && saves == 1) FirstSaveCompleted = true;
            return ValueTask.FromResult(result);
        }
    }
}
