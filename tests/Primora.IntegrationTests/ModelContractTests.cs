using Microsoft.EntityFrameworkCore;
using Primora.Domain.Entities;
using Primora.Infrastructure.Persistence;

namespace Primora.IntegrationTests;

public sealed class ModelContractTests
{
    [Fact]
    public void Model_has_nine_tables_explicit_types_and_scoped_foreign_keys()
    {
        using var context = new PrimoraDbContext(new DbContextOptionsBuilder<PrimoraDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options);
        var entities = context.Model.GetEntityTypes().ToArray();
        Assert.Equal(9, entities.Length);
        Assert.All(entities.SelectMany(entity => entity.GetProperties()), property => Assert.False(property.IsShadowProperty()));
        Assert.All(entities.SelectMany(entity => entity.GetForeignKeys()), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
        foreach (var type in new[] { typeof(EventoCliente), typeof(HistoricoSaudeCliente) })
        {
            var entity = context.Model.FindEntityType(type)!;
            var key = Assert.Single(entity.GetForeignKeys());
            Assert.Equal(new[] { "VersaoId", "UnidadeId", "ClienteId" }, key.Properties.Select(property => property.Name));
        }
        Assert.Equal("timestamp without time zone", context.Model.FindEntityType(typeof(EventoCliente))!.FindProperty("Data")!.GetColumnType());
        Assert.Equal("numeric", context.Model.FindEntityType(typeof(SerieFunilUnidade))!.FindProperty("Valor")!.GetColumnType());
        Assert.Equal(18, context.Model.FindEntityType(typeof(IndicadoresUnidade))!.FindProperty("ReceitaCarteira90d")!.GetPrecision());
        Assert.Equal(2, context.Model.FindEntityType(typeof(EventoCliente))!.FindProperty("Valor")!.GetScale());
    }
}
