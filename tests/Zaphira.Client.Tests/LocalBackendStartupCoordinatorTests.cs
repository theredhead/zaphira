using Zaphira.Client.Backend;

namespace Zaphira.Client.Tests;

public sealed class LocalBackendStartupCoordinatorTests
{
    [Fact]
    public async Task EnsureLocalBackendIsAvailableAsyncDoesNotStartProcessForRemoteBackend()
    {
        FakeBackendConnectionProbe probe = new([BackendConnectionProbeResult.Unavailable]);
        FakeBackendProcessLauncher launcher = new([new FakeBackendProcess(123)]);
        LocalBackendStartupCoordinator coordinator = CreateCoordinator(
            new Uri("https://example.test:5051"),
            probe,
            launcher);

        BackendProcessStatus status = await coordinator.EnsureLocalBackendIsAvailableAsync(CancellationToken.None);

        Assert.Equal(BackendProcessState.NotStarted, status.State);
        Assert.Equal(0, launcher.StartCount);
        Assert.Equal(0, probe.CheckCount);
    }

    [Fact]
    public async Task EnsureLocalBackendIsAvailableAsyncDoesNotStartProcessWhenBackendIsAlreadyReachable()
    {
        FakeBackendConnectionProbe probe = new([BackendConnectionProbeResult.Connected]);
        FakeBackendProcessLauncher launcher = new([new FakeBackendProcess(123)]);
        LocalBackendStartupCoordinator coordinator = CreateCoordinator(
            new Uri("https://localhost:5051"),
            probe,
            launcher);

        BackendProcessStatus status = await coordinator.EnsureLocalBackendIsAvailableAsync(CancellationToken.None);

        Assert.Equal(BackendProcessState.Running, status.State);
        Assert.Equal(BackendOwnership.External, status.Ownership);
        Assert.Equal(0, launcher.StartCount);
        Assert.Equal(1, probe.CheckCount);
    }

    [Fact]
    public async Task EnsureLocalBackendIsAvailableAsyncStartsLocalProcessWhenBackendIsUnavailable()
    {
        FakeBackendConnectionProbe probe = new(
            [
                BackendConnectionProbeResult.Unavailable,
                BackendConnectionProbeResult.Connected
            ]);
        FakeBackendProcessLauncher launcher = new([new FakeBackendProcess(123)]);
        LocalBackendStartupCoordinator coordinator = CreateCoordinator(
            new Uri("https://localhost:5051"),
            probe,
            launcher);

        BackendProcessStatus status = await coordinator.EnsureLocalBackendIsAvailableAsync(CancellationToken.None);

        Assert.Equal(BackendProcessState.Running, status.State);
        Assert.Equal(BackendOwnership.OwnedByClient, status.Ownership);
        Assert.Equal(123, status.ProcessId);
        Assert.Equal(1, launcher.StartCount);
        Assert.Equal(2, probe.CheckCount);
    }

    [Fact]
    public async Task EnsureLocalBackendIsAvailableAsyncReportsFailureWhenStartedBackendDoesNotBecomeReachable()
    {
        FakeBackendConnectionProbe probe = new(
            [
                BackendConnectionProbeResult.Unavailable,
                BackendConnectionProbeResult.Unavailable,
                BackendConnectionProbeResult.Unavailable
            ]);
        FakeBackendProcessLauncher launcher = new([new FakeBackendProcess(123)]);
        LocalBackendStartupCoordinator coordinator = CreateCoordinator(
            new Uri("https://localhost:5051"),
            probe,
            launcher);

        BackendProcessStatus status = await coordinator.EnsureLocalBackendIsAvailableAsync(CancellationToken.None);

        Assert.Equal(BackendProcessState.Failed, status.State);
        Assert.Equal("Local backend started, but did not become reachable.", status.Message);
        Assert.Equal(1, launcher.StartCount);
        Assert.Equal(3, probe.CheckCount);
    }

    private static LocalBackendStartupCoordinator CreateCoordinator(
        Uri backendAddress,
        FakeBackendConnectionProbe probe,
        FakeBackendProcessLauncher launcher)
    {
        LocalBackendProcessManager processManager = new(
            launcher,
            new LocalBackendProcessOptions(
                "/tmp/Zaphira.Server",
                string.Empty,
                "/tmp",
                startupRetryCount: 0,
                TimeSpan.Zero));

        return new LocalBackendStartupCoordinator(
            backendAddress,
            probe,
            processManager,
            readinessCheckCount: 2,
            readinessCheckDelay: TimeSpan.Zero);
    }

    private sealed class FakeBackendConnectionProbe(IReadOnlyList<BackendConnectionProbeResult> results)
        : IBackendConnectionProbe
    {
        private readonly IReadOnlyList<BackendConnectionProbeResult> results = results;

        public int CheckCount { get; private set; }

        public Task<BackendConnectionProbeResult> CheckConnectionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int resultIndex = Math.Min(CheckCount, results.Count - 1);
            CheckCount++;

            return Task.FromResult(results[resultIndex]);
        }
    }

    private sealed class FakeBackendProcessLauncher(IReadOnlyList<object> outcomes) : IBackendProcessLauncher
    {
        private readonly IReadOnlyList<object> outcomes = outcomes;

        public int StartCount { get; private set; }

        public Task<IBackendProcess> StartAsync(LocalBackendProcessStartRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int outcomeIndex = Math.Min(StartCount, outcomes.Count - 1);
            StartCount++;

            return outcomes[outcomeIndex] switch
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

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HasExited = true;

            return Task.CompletedTask;
        }
    }
}
