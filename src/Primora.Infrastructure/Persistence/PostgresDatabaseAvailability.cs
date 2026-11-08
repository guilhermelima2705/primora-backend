using Microsoft.EntityFrameworkCore;
using Primora.Application.Health;

namespace Primora.Infrastructure.Persistence;

public sealed class PostgresDatabaseAvailability(PrimoraDbContext context) : IDatabaseAvailability
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        context.Database.CanConnectAsync(cancellationToken);
}
