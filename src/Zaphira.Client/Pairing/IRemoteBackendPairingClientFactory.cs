namespace Zaphira.Client.Pairing;

public interface IRemoteBackendPairingClientFactory
{
    IRemoteBackendPairingClient Create(Uri backendAddress);
}
