using System.Security.Cryptography;
using System.Text;
using Primora.Application.Importacao;
using Primora.Tests.Fixtures;

namespace Primora.UnitTests;

public sealed class DatasetReaderTests
{
    private readonly LeitorDataset reader = new();

    [Fact]
    public void Validates_every_section_and_preserves_aggregates_positions_and_original_events()
    {
        var bytes = DatasetFixture.Bytes(DatasetFixture.Create());
        var before = bytes.ToArray();
        var data = reader.Preparar(bytes, "fixture.json");
        Assert.Equal(new ContagensDataset(1, 2, 2, 1, 2, 2, 60, 60, 4), data.Contagens);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), data.Versao.HashSha256);
        Assert.Equal(before, bytes);
        Assert.Equal(20, data.Indicadores.Sum(x => x.CarteiraTotalClientes));
        Assert.Single(data.Clientes);
        Assert.Equal(Enumerable.Range(1, 30), data.Engajamento.Where(x => x.UnidadeId == "unidade-a").Select(x => x.Posicao));
        Assert.All(data.Funil, point => Assert.Equal(DatasetFixture.PreciseRatio, point.Valor));
        Assert.Equal(98.76m, data.Eventos[0].Valor);
        Assert.Null(data.Eventos[1].Valor);
        Assert.Equal(new[] { 1, 2 }, data.Eventos.Select(x => x.OrdemOrigem));
        Assert.All(data.Eventos, item => Assert.Equal(DateTimeKind.Unspecified, item.Data.Kind));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"geradoEm\":\"2026-09-01\",\"geradoEm\":\"2026-09-02\",\"tenants\":[],\"clientes\":[]}")]
    public void Rejects_invalid_json_or_duplicate_properties(string json) =>
        Assert.Throws<DatasetInvalidoException>(() => reader.Preparar(Encoding.UTF8.GetBytes(json), "invalid.json"));

    [Theory]
    [InlineData("missing-root") ]
    [InlineData("unknown-root") ]
    [InlineData("null-array") ]
    [InlineData("no-units") ]
    [InlineData("non-object") ]
    [InlineData("duplicate-unit") ]
    [InlineData("duplicate-client") ]
    [InlineData("invalid-tenant") ]
    [InlineData("tenant-name") ]
    [InlineData("missing-counter") ]
    [InlineData("unknown-nested") ]
    [InlineData("money-scale") ]
    [InlineData("money-overflow") ]
    [InlineData("money-string") ]
    [InlineData("negative-counter") ]
    [InlineData("distribution") ]
    [InlineData("absolute-risk") ]
    [InlineData("series-length") ]
    [InlineData("ratio-range") ]
    [InlineData("decimal-loss") ]
    [InlineData("score-range") ]
    [InlineData("duplicate-date") ]
    [InlineData("date-order") ]
    [InlineData("event-timezone") ]
    [InlineData("event-precision") ]
    [InlineData("late-invalid-event") ]
    public void Rejects_incompatible_data_before_persistence(string scenario) =>
        Assert.Throws<DatasetInvalidoException>(() => reader.Preparar(DatasetFixture.Bytes(DatasetFixture.Invalid(scenario)), "invalid.json"));

    [Fact]
    public void Bom_is_preserved_in_the_hash_but_does_not_prevent_parsing()
    {
        var bytes = DatasetFixture.Bytes(DatasetFixture.Create());
        var withBom = new byte[] { 0xef, 0xbb, 0xbf }.Concat(bytes).ToArray();
        var first = reader.Preparar(bytes, "a.json");
        var second = reader.Preparar(withBom, "b.json");
        Assert.Equal(first.Contagens, second.Contagens);
        Assert.NotEqual(first.Versao.HashSha256, second.Versao.HashSha256);
    }
}
