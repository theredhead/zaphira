namespace Zaphira.Client.Backend;

public sealed class LocalBackendProcessManager
{
    private readonly IBackendProcessLauncher processLauncher;
    private readonly LocalBackendProcessOptions options;
    private IBackendProcess ownedProcess = NoBackendProcess.Instance;
    private int externalProcessId;

    public LocalBackendProcessManager(IBackendProcessLauncher processLauncher, LocalBackendProcessOptions options)
    {
        ArgumentNullException.ThrowIfNull(processLauncher);
        ArgumentNullException.ThrowIfNull(options);

        this.processLauncher = processLauncher;
        this.options = options;
    }

    public async Task<BackendProcessStatus> StartAsync(CancellationToken cancellationToken)
    {
        if (ownedProcess.ProcessId > 0 && !ownedProcess.HasExited)
        {
            return BackendProcessStatus.RunningOwned(ownedProcess.ProcessId);
        }

        externalProcessId = 0;
        string failureMessage = "Backend failed to start.";
        LocalBackendProcessStartRequest request = new(options.ExecutablePath, options.Arguments, options.WorkingDirectory);

        for (int attempt = 0; attempt <= options.StartupRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                ownedProcess = await processLauncher.StartAsync(request, cancellationToken);
                return BackendProcessStatus.RunningOwned(ownedProcess.ProcessId);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failureMessage = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Backend failed to start."
                    : exception.Message;

                if (attempt < options.StartupRetryCount)
                {
                    await Task.Delay(options.StartupRetryDelay, cancellationToken);
                }
            }
        }

        ownedProcess = NoBackendProcess.Instance;
        return BackendProcessStatus.Failed(failureMessage);
    }

    public void UseExternalBackend(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        ownedProcess = NoBackendProcess.Instance;
        externalProcessId = processId;
    }

    public async Task<BackendProcessStatus> StopAsync(CancellationToken cancellationToken)
    {
        if (ownedProcess.ProcessId > 0 && !ownedProcess.HasExited)
        {
            await ownedProcess.StopAsync(cancellationToken);
            ownedProcess = NoBackendProcess.Instance;

            return BackendProcessStatus.Stopped();
        }

        if (externalProcessId > 0)
        {
            return BackendProcessStatus.RunningExternal(externalProcessId);
        }

        return BackendProcessStatus.Stopped();
    }

    private sealed class NoBackendProcess : IBackendProcess
    {
        public static NoBackendProcess Instance { get; } = new();

        public int ProcessId => 0;

        public bool HasExited => true;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }
}
