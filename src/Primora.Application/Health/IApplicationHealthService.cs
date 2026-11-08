namespace Primora.Application.Health;

public interface IApplicationHealthService
{
    Task<HealthReport> CheckAsync(CancellationToken cancellationToken);
}
