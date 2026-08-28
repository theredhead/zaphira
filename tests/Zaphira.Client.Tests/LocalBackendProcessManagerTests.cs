using Zaphira.Client.Backend;

namespace Zaphira.Client.Tests;

public sealed class LocalBackendProcessManagerTests
{
    [Fact]
    public async Task StartAsyncLaunchesOwnedBackendProcess()
    {
        FakeBackendProcessLauncher launcher = new([new FakeBackendProcess(123)]);
        LocalBackendProcessManager manager = new(
            launcher,
            new LocalBackendProcessOptions("/tmp/Zaphira.Server", "--urls https://localhost:5051", "/tmp", 0, TimeSpan.Zero));

        BackendProcessStatus status = await manager.StartAsync(CancellationToken.None);

        Assert.Equal(BackendProcessState.Running, status.State);
        Assert.Equal(BackendOwnership.OwnedByClient, status.Ownership);
        Assert.Equal(123, status.ProcessId);
        Assert.Equal(1, launcher.Attempts);
    }

    [Fact]
    public async Task StopAsyncStopsOwnedBackendProcess()
    {
        FakeBackendProcess process = new(123);
        LocalBackendProcessManager manager = new(
            new FakeBackendProcessLauncher([process]),
            new LocalBackendProcessOptions("/tmp/Zaphira.Server", string.Empty, "/tmp", 0, TimeSpan.Zero));

        await manager.StartAsync(CancellationToken.None);
        await manager.StopAsync(CancellationToken.None);

        Assert.True(process.StopWasRequested);
    }

    [Fact]
    public async Task StopAsyncDoesNotStopExternalBackendProcess()
    {
        FakeBackendProcess process = new(123);
        LocalBackendProcessManager manager = new(
            new FakeBackendProcessLauncher([process]),
            new LocalBackendProcessOptions("/tmp/Zaphira.Server", string.Empty, "/tmp", 0, TimeSpan.Zero));

        manager.UseExternalBackend(123);
        await manager.StopAsync(CancellationToken.None);

        Assert.False(process.StopWasRequested);
    }

    [Fact]
    public async Task StartAsyncRetriesStartupFailures()
    {
        FakeBackendProcessLauncher launcher = new([new InvalidOperationException("First failure."), new FakeBackendProcess(123)]);
        LocalBackendProcessManager manager = new(
            launcher,
            new LocalBackendProcessOptions("/tmp/Zaphira.Server", string.Empty, "/tmp", 1, TimeSpan.Zero));

        BackendProcessStatus status = await manager.StartAsync(CancellationToken.None);

        Assert.Equal(BackendProcessState.Running, status.State);
        Assert.Equal(2, launcher.Attempts);
    }

    [Fact]
    public async Task StartAsyncReturnsFailureAfterRetryLimit()
    {
        FakeBackendProcessLauncher launcher = new([new InvalidOperationException("No server.")]);
        LocalBackendProcessManager manager = new(
            launcher,
            new LocalBackendProcessOptions("/tmp/Zaphira.Server", string.Empty, "/tmp", 0, TimeSpan.Zero));

        BackendProcessStatus status = await manager.StartAsync(CancellationToken.None);

        Assert.Equal(BackendProcessState.Failed, status.State);
        Assert.Equal(BackendOwnership.None, status.Ownership);
        Assert.Equal("No server.", status.Message);
    }

    private sealed class FakeBackendProcessLauncher(IReadOnlyList<object> outcomes) : IBackendProcessLauncher
    {
        public int Attempts { get; private set; }

        public Task<IBackendProcess> StartAsync(LocalBackendProcessStartRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            object outcome = outcomes[Math.Min(Attempts, outcomes.Count - 1)];
            Attempts++;

            return outcome switch
            {
                IBackendProcess process => Task.FromResult(process),
                Exception exception => Task.FromException<IBackendProcess>(exception),
                _ => Task.FromException<IBackendProcess>(new InvalidOperationException("Unsupported fake outcome."))
            };
        }
    }

    private sealed class FakeBackendProcess(int processId) : IBackendProcess
    {
        public int ProcessId { get; } = processId;

        public bool HasExited { get; private set; }

        public bool StopWasRequested { get; private set; }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StopWasRequested = true;
            HasExited = true;

            return Task.CompletedTask;
        }
    }
}
