using Primora.Application.Health;

namespace Primora.UnitTests;

public sealed class ApplicationHealthServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reports_the_database_result(bool available)
    {
        var service = new ApplicationHealthService(new DatabaseStub(available));
        var report = await service.CheckAsync(CancellationToken.None);
        Assert.Equal(available, report.DatabaseAvailable);
    }

    [Fact]
    public async Task Propagates_cancellation_to_the_database()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new ApplicationHealthService(new DatabaseStub(true));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CheckAsync(cancellation.Token));
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
