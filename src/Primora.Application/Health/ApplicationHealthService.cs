namespace Primora.Application.Health;

public sealed class ApplicationHealthService(IDatabaseAvailability database) : IApplicationHealthService
{
    public async Task<HealthReport> CheckAsync(CancellationToken cancellationToken) =>
        new(await database.CanConnectAsync(cancellationToken));
}
