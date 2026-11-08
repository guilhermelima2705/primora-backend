using Microsoft.Extensions.DependencyInjection;
using Primora.Application.Health;
using Primora.Application.Importacao;

namespace Primora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddSingleton<LeitorDataset>()
            .AddScoped<IApplicationHealthService, ApplicationHealthService>();
}
