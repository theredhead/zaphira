namespace Zaphira.Client.Backend;

public interface IBackendConnectionProbe
{
    Task<BackendConnectionProbeResult> CheckConnectionAsync(CancellationToken cancellationToken);
}
