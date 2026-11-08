namespace Primora.Application.Health;

public interface IDatabaseAvailability
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
