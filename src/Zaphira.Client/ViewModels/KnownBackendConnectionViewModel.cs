using Zaphira.Client.Security;

namespace Zaphira.Client.ViewModels;

public sealed class KnownBackendConnectionViewModel : ViewModelBase
{
    public KnownBackendConnectionViewModel(TrustedBackendConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        BackendAddress = connection.BackendAddress.ToString();
        CertificateThumbprint = connection.CertificateThumbprint;
        Description = connection.Description;
        PairingId = connection.PairingId;
        AccessToken = connection.AccessToken;
    }

    public string BackendAddress { get; }

    public string CertificateThumbprint { get; }

    public string Description { get; }

    public Guid PairingId { get; }

    public string AccessToken { get; }

    public Uri ToBackendAddress() => new(BackendAddress);
}
