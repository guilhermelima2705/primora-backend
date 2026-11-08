using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primora.Application.Health;

namespace Primora.IntegrationTests;

public sealed class PrimoraApiFactory(bool available = true, bool realDatabase = false,
    string environment = "Development") : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        if (!realDatabase)
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Primora"] = "Host=localhost;Database=unused;Username=unused"
                }));
        }
        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TestFailuresController).Assembly);
            if (!realDatabase)
            {
                services.RemoveAll<IDatabaseAvailability>();
                services.AddScoped<IDatabaseAvailability>(_ => new DatabaseStub(available));
            }
        });
    }

    private sealed class DatabaseStub(bool available) : IDatabaseAvailability
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(available);
        }
    }
}
