namespace Zaphira.Client.Backend;

public interface IBackendProcessLauncher
{
    Task<IBackendProcess> StartAsync(LocalBackendProcessStartRequest request, CancellationToken cancellationToken);
}
