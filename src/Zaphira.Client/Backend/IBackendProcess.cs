namespace Zaphira.Client.Backend;

public interface IBackendProcess
{
    int ProcessId { get; }

    bool HasExited { get; }

    Task StopAsync(CancellationToken cancellationToken);
}
