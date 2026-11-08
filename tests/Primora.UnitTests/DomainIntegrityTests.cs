using Primora.Domain.Entities;

namespace Primora.UnitTests;

public sealed class DomainIntegrityTests
{
    private static readonly Guid Versao = Guid.NewGuid();

    [Fact]
    public void Preserves_textual_ids_and_original_values()
    {
        var unit = new Unidade(Versao, "vetor-sul", "Vetor Sul", "media", 1);
        var client = new Cliente(Versao, unit.Id, "cli-timeline-001", "Comercial Rio Bonito", 1);
        Assert.Equal("vetor-sul", client.UnidadeId);
        Assert.Equal("cli-timeline-001", client.Id);
        Assert.Equal(Versao, client.VersaoId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Rejects_invalid_series_positions(int position)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerieEngajamentoUnidade(Versao, "u", position, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerieFunilUnidade(Versao, "u", position, 0.5m));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Rejects_invalid_health_scores(int score) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new HistoricoSaudeCliente(Versao, "u", "c", new DateOnly(2026, 9, 1), score));

    [Fact]
    public void Does_not_assign_a_timezone_to_events()
    {
        var date = new DateTime(2026, 9, 1, 9, 12, 0, DateTimeKind.Unspecified);
        var item = new EventoCliente(Versao, "u", "c", 1, date, "compra", "Pedido", 123.45m);
        Assert.Equal(date, item.Data);
        Assert.Equal(DateTimeKind.Unspecified, item.Data.Kind);
        Assert.Throws<ArgumentException>(() => new EventoCliente(Versao, "u", "c", 1,
            DateTime.SpecifyKind(date, DateTimeKind.Utc), "compra", "Pedido", null));
    }

    [Fact]
    public void Rejects_inconsistent_aggregate_distribution_and_absolute_risk()
    {
        Assert.Throws<ArgumentException>(() => new IndicadoresUnidade(Versao, "u", 11, 0m, 0, 0, 0, 0, 0, 0, 4, 3, 2, 1, 3));
        Assert.Throws<ArgumentException>(() => new IndicadoresUnidade(Versao, "u", 10, 0m, 0, 0, 0, 0, 0, 0, 4, 3, 2, 1, 4));
    }

    [Fact]
    public void Preserves_decimal_precision_for_proportions()
    {
        const decimal value = 0.1234567890123456789012345678m;
        var point = new SerieFunilUnidade(Versao, "u", 1, value);
        Assert.Equal(value, point.Valor);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerieFunilUnidade(Versao, "u", 1, 1.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventoCliente(Versao, "u", "c", 1,
            new DateTime(2026, 9, 1), "compra", "Pedido", -1m));
    }
}
