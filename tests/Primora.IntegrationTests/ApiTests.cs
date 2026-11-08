using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Primora.IntegrationTests;

public sealed class ApiTests
{
    private static HttpClient CreateClient(PrimoraApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    [Fact]
    public async Task Health_returns_application_and_database_availability()
    {
        using var factory = new PrimoraApiFactory();
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("available", body.RootElement.GetProperty("application").GetString());
        Assert.Equal("available", body.RootElement.GetProperty("database").GetString());
    }

    [Fact]
    public async Task Unavailable_database_returns_503_problem_details()
    {
        using var factory = new PrimoraApiFactory(available: false);
        using var client = CreateClient(factory);
        await AssertProblem(client, "/health", HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData("/__tests/failure", HttpStatusCode.InternalServerError)]
    [InlineData("/__tests/bad-request", HttpStatusCode.BadRequest)]
    [InlineData("/does-not-exist", HttpStatusCode.NotFound)]
    public async Task Errors_are_standardized_and_do_not_expose_exception_details(string path, HttpStatusCode status)
    {
        using var factory = new PrimoraApiFactory();
        using var client = CreateClient(factory);
        await AssertProblem(client, path, status);
    }

    [Fact]
    public async Task OpenApi_describes_the_health_endpoint()
    {
        using var factory = new PrimoraApiFactory();
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var endpoint = body.RootElement.GetProperty("paths").GetProperty("/health").GetProperty("get");
        Assert.True(endpoint.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(endpoint.GetProperty("responses").TryGetProperty("503", out _));
    }

    [Fact]
    public async Task OpenApi_is_not_exposed_in_production()
    {
        using var factory = new PrimoraApiFactory(environment: "Production");
        using var client = CreateClient(factory);
        await AssertProblem(client, "/openapi/v1.json", HttpStatusCode.NotFound);
    }

    [Fact]
    public void Missing_connection_configuration_prevents_startup()
    {
        using var factory = new PrimoraApiFactory();
        using var invalidConfiguration = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Primora"] = string.Empty
            })));
        Assert.Throws<OptionsValidationException>(() => invalidConfiguration.CreateClient());
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Health_verifies_the_real_PostgreSql_connection_without_creating_tables()
    {
        using var factory = new PrimoraApiFactory(realDatabase: true);
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("available", body.RootElement.GetProperty("database").GetString());
    }

    private static async Task AssertProblem(HttpClient client, string path, HttpStatusCode expected)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("sensitive-test-marker", text);
        Assert.DoesNotContain("Password=", text);
        using var body = JsonDocument.Parse(text);
        Assert.Equal((int)expected, body.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("traceId").GetString()));
    }
}
