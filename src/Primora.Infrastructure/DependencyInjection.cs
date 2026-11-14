using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Primora.Application.Dataset;
using Primora.Application.Health;
using Primora.Application.Importacao;
using Primora.Infrastructure.Importacao;
using Primora.Infrastructure.Persistence;

namespace Primora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration("ConnectionStrings")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Primora),
                "Configure ConnectionStrings:Primora por variável de ambiente ou User Secrets.")
            .ValidateOnStart();

        services.AddDbContext<PrimoraDbContext>((provider, options) =>
        {
            var configuration = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connection = new NpgsqlConnectionStringBuilder(configuration.Primora)
            {
                Timeout = 5,
                CommandTimeout = 5
            };
            options.UseNpgsql(connection.ConnectionString);
        });
        services.AddScoped<IDatabaseAvailability, PostgresDatabaseAvailability>();
        services.AddScoped<IDatasetImportador, EfDatasetImportador>();
        services.AddOptions<DatasetPublicacaoOptions>()
            .BindConfiguration("DatasetPublicacao")
            .Validate(options => options.HashSha256.Length == 64 &&
                options.HashSha256.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'),
                "Configure DatasetPublicacao:HashSha256 com o SHA-256 em minúsculas da versão publicada.")
            .ValidateOnStart();
        services.AddScoped<IDatasetConsultas, EfDatasetConsultas>();
        return services;
    }
}
