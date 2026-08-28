namespace Zaphira.Client.Backend;

public sealed class LocalBackendStartupCoordinator
{
    private readonly Uri backendAddress;
    private readonly IBackendConnectionProbe backendConnectionProbe;
    private readonly LocalBackendProcessManager processManager;
    private readonly int readinessCheckCount;
    private readonly TimeSpan readinessCheckDelay;

    public LocalBackendStartupCoordinator(
        Uri backendAddress,
        IBackendConnectionProbe backendConnectionProbe,
        LocalBackendProcessManager processManager,
        int readinessCheckCount,
        TimeSpan readinessCheckDelay)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);
        ArgumentNullException.ThrowIfNull(backendConnectionProbe);
        ArgumentNullException.ThrowIfNull(processManager);
        ArgumentOutOfRangeException.ThrowIfNegative(readinessCheckCount);

        if (readinessCheckDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(readinessCheckDelay), "Readiness check delay cannot be negative.");
        }

        this.backendAddress = backendAddress;
        this.backendConnectionProbe = backendConnectionProbe;
        this.processManager = processManager;
        this.readinessCheckCount = readinessCheckCount;
        this.readinessCheckDelay = readinessCheckDelay;
    }

    public async Task<BackendProcessStatus> EnsureLocalBackendIsAvailableAsync(CancellationToken cancellationToken)
    {
        if (!IsLocalBackendAddress(backendAddress))
        {
            return BackendProcessStatus.LocalBackendNotRequired();
        }

        if (await backendConnectionProbe.CheckConnectionAsync(cancellationToken) == BackendConnectionProbeResult.Connected)
        {
            return BackendProcessStatus.BackendAlreadyAvailable();
        }

        BackendProcessStatus startStatus = await processManager.StartAsync(cancellationToken);
        if (startStatus.State == BackendProcessState.Failed)
        {
            return startStatus;
        }

        return await WaitForBackendAvailabilityAsync(startStatus, cancellationToken);
    }

    private async Task<BackendProcessStatus> WaitForBackendAvailabilityAsync(
        BackendProcessStatus startStatus,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < readinessCheckCount; attempt++)
        {
            if (await backendConnectionProbe.CheckConnectionAsync(cancellationToken) == BackendConnectionProbeResult.Connected)
            {
                return startStatus;
            }

            if (attempt + 1 < readinessCheckCount)
            {
                await Task.Delay(readinessCheckDelay, cancellationToken);
            }
        }

        return BackendProcessStatus.Failed("Local backend started, but did not become reachable.");
    }

    private static bool IsLocalBackendAddress(Uri address) =>
        address.IsLoopback
        || string.Equals(address.Host, "localhost", StringComparison.OrdinalIgnoreCase);
}
