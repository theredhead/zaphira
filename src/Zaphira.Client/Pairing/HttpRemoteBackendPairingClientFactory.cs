namespace Zaphira.Client.Pairing;

public sealed class HttpRemoteBackendPairingClientFactory : IRemoteBackendPairingClientFactory
{
    public IRemoteBackendPairingClient Create(Uri backendAddress)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);

        HttpClientHandler handler = new()
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        return new HttpRemoteBackendPairingClient(new HttpClient(handler)
        {
            BaseAddress = backendAddress
        });
    }
}
