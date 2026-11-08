using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Primora.Domain.Entities;
using Primora.Infrastructure.Persistence;

namespace Primora.IntegrationTests;

[Trait("Category", "PostgreSQL")]
public sealed class PostgresIntegrityTests : IAsyncLifetime
{
    private PrimoraApiFactory? factory;
    private AsyncServiceScope scope;
    private PrimoraDbContext database = null!;
    private IDbContextTransaction? transaction;
    private readonly Guid version = Guid.NewGuid();
    private const string UnitA = "unidade-integridade-a";
    private const string UnitB = "unidade-integridade-b";
    private const string Client = "cliente-integridade";
    private static readonly DateTime EventDate = new(2026, 9, 1, 9, 12, 0, DateTimeKind.Unspecified);
    private string Hash => version.ToString("N").PadRight(64, '0');

    public async Task InitializeAsync()
    {
        factory = new PrimoraApiFactory(realDatabase: true);
        scope = factory.Services.CreateAsyncScope();
        database = scope.ServiceProvider.GetRequiredService<PrimoraDbContext>();
        transaction = await database.Database.BeginTransactionAsync();
        database.VersoesDataset.Add(CreateVersion(version, Hash));
        database.Unidades.AddRange(new Unidade(version, UnitA, "Unidade de teste A", "media", 1),
            new Unidade(version, UnitB, "Unidade de teste B", "pequena", 2));
        database.Clientes.Add(new Cliente(version, UnitA, Client, "Cliente de teste", 1));
        database.IndicadoresUnidades.Add(new IndicadoresUnidade(version, UnitA, 10, 12345.67m, 20, 2,
            10, 8, 4, 2, 4, 3, 2, 1, 3));
        database.EventosClientes.Add(new EventoCliente(version, UnitA, Client, 1, EventDate, "compra", "Evento de teste", 98.76m));
        database.HistoricosSaude.Add(new HistoricoSaudeCliente(version, UnitA, Client, new DateOnly(2026, 9, 1), 47));
        database.FatoresRisco.Add(new FatorRiscoUnidade(version, UnitA, 1, "Fator textual de teste"));
        await database.SaveChangesAsync();
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
    }

    [Fact]
    public async Task Schema_contains_nine_domain_tables_and_the_applied_migration()
    {
        var tables = await database.Database.SqlQueryRaw<string>(
            "SELECT tablename AS \"Value\" FROM pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'")
            .ToListAsync();
        Assert.Equal(new[] { "clientes", "eventos_cliente", "fatores_risco_unidade", "historico_saude_cliente",
            "indicadores_unidade", "serie_engajamento_unidade", "serie_funil_unidade", "unidades", "versoes_dataset" },
            tables.Order(StringComparer.Ordinal));
        var migrations = await database.Database.GetAppliedMigrationsAsync();
        Assert.EndsWith("_ModeloInicial", Assert.Single(migrations));
        Assert.Empty(await database.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Event_cannot_reference_a_client_from_another_unit()
    {
        var eventDate = new NpgsqlParameter("event_date", NpgsqlDbType.Timestamp) { Value = EventDate };
        await AssertSqlFailure(() => database.Database.ExecuteSqlRawAsync("""
            INSERT INTO public.eventos_cliente (versao_id, unidade_id, cliente_id, ordem_origem, data, tipo, titulo)
            VALUES ({0}, {1}, {2}, 2, {3}, 'nota', 'Teste')
            """, version, UnitB, Client, eventDate), PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task Health_history_cannot_reference_a_client_from_another_version()
    {
        var otherVersion = Guid.NewGuid();
        database.VersoesDataset.Add(CreateVersion(otherVersion, otherVersion.ToString("N").PadRight(64, '0')));
        database.Unidades.Add(new Unidade(otherVersion, UnitA, "Outra versão", "media", 1));
        await database.SaveChangesAsync();
        await AssertSqlFailure(() => database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.historico_saude_cliente (versao_id, unidade_id, cliente_id, data, score)
            VALUES ({otherVersion}, {UnitA}, {Client}, {new DateOnly(2026, 9, 1)}, 50)
            """), PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task Indicators_require_an_existing_unit()
    {
        database.IndicadoresUnidades.Add(new IndicadoresUnidade(version, "unidade-inexistente", 0, 0m,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        await AssertSqlFailure(() => database.SaveChangesAsync(), PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task Textual_client_ids_are_scoped_by_unit_and_version()
    {
        var otherVersion = Guid.NewGuid();
        database.VersoesDataset.Add(CreateVersion(otherVersion, otherVersion.ToString("N").PadRight(64, '0')));
        database.Unidades.Add(new Unidade(otherVersion, UnitA, "Outra versão", "media", 1));
        database.Clientes.AddRange(new Cliente(version, UnitB, Client, "Mesmo ID em outra unidade", 2),
            new Cliente(otherVersion, UnitA, Client, "Mesmo ID em outra versão", 1));
        await database.SaveChangesAsync();
        Assert.Equal(3, await database.Clientes.CountAsync(x => x.Id == Client &&
            (x.VersaoId == version || x.VersaoId == otherVersion)));
    }

    [Fact]
    public async Task Dataset_hash_is_unique()
    {
        database.VersoesDataset.Add(CreateVersion(Guid.NewGuid(), Hash));
        await AssertSqlFailure(() => database.SaveChangesAsync(), PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Series_position_is_unique_within_the_same_unit_and_version()
    {
        database.SeriesEngajamento.Add(new SerieEngajamentoUnidade(version, UnitA, 1, 10));
        await database.SaveChangesAsync();
        await AssertSqlFailure(() => database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.serie_engajamento_unidade (versao_id, unidade_id, posicao, valor)
            VALUES ({version}, {UnitA}, 1, 11)
            """), PostgresErrorCodes.UniqueViolation);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 31)]
    [InlineData(true, 0)]
    [InlineData(true, 31)]
    public async Task Database_rejects_invalid_series_positions(bool funnel, int position)
    {
        if (funnel)
            await AssertSqlFailure(() => database.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO public.serie_funil_unidade (versao_id, unidade_id, posicao, valor)
                VALUES ({version}, {UnitA}, {position}, 0.5)
                """), PostgresErrorCodes.CheckViolation);
        else
            await AssertSqlFailure(() => database.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO public.serie_engajamento_unidade (versao_id, unidade_id, posicao, valor)
                VALUES ({version}, {UnitA}, {position}, 10)
                """), PostgresErrorCodes.CheckViolation);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Database_rejects_invalid_health_scores(int score)
    {
        await AssertSqlFailure(() => database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.historico_saude_cliente (versao_id, unidade_id, cliente_id, data, score)
            VALUES ({version}, {UnitA}, {Client}, {new DateOnly(2026, 9, 2)}, {score})
            """), PostgresErrorCodes.CheckViolation);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.01")]
    public async Task Database_rejects_out_of_range_proportions(string value)
    {
        var proportion = decimal.Parse(value, CultureInfo.InvariantCulture);
        await AssertSqlFailure(() => database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.serie_funil_unidade (versao_id, unidade_id, posicao, valor)
            VALUES ({version}, {UnitA}, 1, {proportion})
            """), PostgresErrorCodes.CheckViolation);
    }

    [Theory]
    [InlineData("UPDATE public.indicadores_unidade SET carteira_total_clientes=-1 WHERE versao_id={0} AND unidade_id={1}")]
    [InlineData("UPDATE public.indicadores_unidade SET receita_carteira_90d=-1 WHERE versao_id={0} AND unidade_id={1}")]
    [InlineData("UPDATE public.indicadores_unidade SET funil_gerada_90d=-1 WHERE versao_id={0} AND unidade_id={1}")]
    [InlineData("UPDATE public.indicadores_unidade SET saudavel=5 WHERE versao_id={0} AND unidade_id={1}")]
    [InlineData("UPDATE public.indicadores_unidade SET risco_absoluto=4 WHERE versao_id={0} AND unidade_id={1}")]
    public async Task Database_enforces_aggregate_consistency_and_nonnegative_values(string sql)
    {
        await AssertSqlFailure(() => database.Database.ExecuteSqlRawAsync(sql, version, UnitA), PostgresErrorCodes.CheckViolation);
    }

    [Theory]
    [InlineData("DELETE FROM public.versoes_dataset WHERE id={0}")]
    [InlineData("DELETE FROM public.unidades WHERE versao_id={0} AND id={1}")]
    [InlineData("DELETE FROM public.clientes WHERE versao_id={0} AND unidade_id={1} AND id={2}")]
    public async Task Database_restricts_deletion_of_referenced_records(string sql)
    {
        await AssertSqlFailure(() => database.Database.ExecuteSqlRawAsync(sql, version, UnitA, Client), PostgresErrorCodes.RestrictViolation);
    }

    [Fact]
    public async Task Database_rejects_blank_risk_factors()
    {
        await AssertSqlFailure(() => database.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.fatores_risco_unidade SET descricao='' WHERE versao_id={version} AND unidade_id={UnitA}
            """), PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task Monetary_values_proportions_dates_and_event_hours_roundtrip_without_invented_timezone()
    {
        const decimal proportion = 0.1234567890123456789012345678m;
        database.SeriesFunil.Add(new SerieFunilUnidade(version, UnitA, 1, proportion));
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var indicators = await database.IndicadoresUnidades.SingleAsync(x => x.VersaoId == version);
        var point = await database.SeriesFunil.SingleAsync(x => x.VersaoId == version);
        var item = await database.EventosClientes.SingleAsync(x => x.VersaoId == version);
        var score = await database.HistoricosSaude.SingleAsync(x => x.VersaoId == version);
        Assert.Equal(12345.67m, indicators.ReceitaCarteira90d);
        Assert.Equal(proportion, point.Valor);
        Assert.Equal(98.76m, item.Valor);
        Assert.Equal(EventDate, item.Data);
        Assert.Equal(DateTimeKind.Unspecified, item.Data.Kind);
        Assert.Equal(new DateOnly(2026, 9, 1), score.Data);
    }

    private static VersaoDataset CreateVersion(Guid id, string hash) =>
        new(id, new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow, hash, "fixture-integridade", "teste");

    private static async Task AssertSqlFailure(Func<Task> action, string expectedSqlState)
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(action);
        var postgresError = error as PostgresException ?? error.InnerException as PostgresException;
        Assert.NotNull(postgresError);
        Assert.Equal(expectedSqlState, postgresError.SqlState);
    }
}
