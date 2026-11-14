using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primora.Application.Dataset;

namespace Primora.IntegrationTests;

public sealed class DatasetEndpointsTests
{
    [Theory]
    [InlineData("/unidades")]
    [InlineData("/clientes")]
    public async Task Missing_published_version_returns_empty_array(string path)
    {
        using var factory = new PrimoraApiFactory();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDatasetConsultas>();
            services.AddSingleton<IDatasetConsultas>(new ConsultasStub(empty: true));
        }));
        using var client = app.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.Contains("X-Dataset-Gerado-Em"));
    }

    [Theory]
    [InlineData("/unidades")]
    [InlineData("/clientes")]
    public async Task Query_failure_returns_problem_details(string path)
    {
        using var factory = new PrimoraApiFactory();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IDatasetConsultas>(new ConsultasStub(fail: true))));
        using var client = app.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("detalhe interno de teste", body);
    }

    [Fact]
    public async Task Client_payload_preserves_dates_order_and_optional_value()
    {
        using var factory = new PrimoraApiFactory();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IDatasetConsultas>(new ConsultasStub())));
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/clientes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2026-09-01", Assert.Single(response.Headers.GetValues("X-Dataset-Gerado-Em")));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal("unidade-a", data.GetProperty("tenantId").GetString());
        Assert.Equal("Unidade A", data.GetProperty("tenantNome").GetString());
        Assert.Equal("2026-09-01", data.GetProperty("healthSeries")[0].GetProperty("data").GetString());
        var eventos = data.GetProperty("eventos");
        Assert.Equal("2026-09-01T09:12:00.123456", eventos[0].GetProperty("data").GetString());
        Assert.False(eventos[0].TryGetProperty("valor", out _));
        Assert.Equal(0m, eventos[1].GetProperty("valor").GetDecimal());
    }

    [Theory]
    [InlineData("Development", "http://localhost:5173", true)]
    [InlineData("Development", "http://127.0.0.1:5173", true)]
    [InlineData("Development", "https://other.example", false)]
    [InlineData("Production", "http://localhost:5173", false)]
    [InlineData("Production", "https://frontend.example", true)]
    public async Task Cors_allows_only_configured_origins(string environment, string origin, bool allowed)
    {
        using var factory = new PrimoraApiFactory(environment: environment);
        using var app = factory.WithWebHostBuilder(builder =>
        {
            if (environment == "Production")
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://frontend.example" }));
            builder.ConfigureServices(services => services.AddSingleton<IDatasetConsultas>(new ConsultasStub()));
        });
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/clientes");
        request.Headers.Add("Origin", origin);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(allowed, response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
        if (allowed)
        {
            Assert.Equal(origin, Assert.Single(origins!));
            Assert.Contains("X-Dataset-Gerado-Em", response.Headers.GetValues("Access-Control-Expose-Headers"));
        }
    }

    private sealed class ConsultasStub(bool empty = false, bool fail = false) : IDatasetConsultas
    {
        private static readonly DateOnly GeradoEm = new(2026, 9, 1);

        public Task<ListagemDataset<UnidadeDto>> ListarUnidadesAsync(CancellationToken cancellationToken) =>
            Resultado<UnidadeDto>([]);

        public Task<ListagemDataset<ClienteDto>> ListarClientesAsync(CancellationToken cancellationToken)
        {
            var horario = new DateTime(2026, 9, 1, 9, 12, 0).AddTicks(1_234_560);
            return Resultado<ClienteDto>([
                new("cliente-a", "Cliente A", "unidade-a", "Unidade A", [new(GeradoEm, 47)],
                    [new(horario, "nota", "Sem valor", null), new(horario, "compra", "Com zero", 0m)])
            ]);
        }

        private Task<ListagemDataset<T>> Resultado<T>(IReadOnlyList<T> itens) => fail
            ? Task.FromException<ListagemDataset<T>>(new InvalidOperationException("detalhe interno de teste"))
            : Task.FromResult<ListagemDataset<T>>(empty ? new(null, []) : new(GeradoEm, itens));
    }
}
