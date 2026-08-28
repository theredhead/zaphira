namespace Zaphira.Client.Security;

public sealed record TrustedBackendConnection
{
    public static Guid NoPairingId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public TrustedBackendConnection(Uri backendAddress, string certificateThumbprint, string description)
        : this(backendAddress, certificateThumbprint, description, NoPairingId, "__zaphira_no_pairing_token__")
    {
    }

    public TrustedBackendConnection(
        Uri backendAddress,
        string certificateThumbprint,
        string description,
        Guid pairingId,
        string accessToken)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateThumbprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        if (!backendAddress.IsAbsoluteUri)
        {
            throw new ArgumentException("Backend address must be absolute.", nameof(backendAddress));
        }

        BackendAddress = backendAddress;
        CertificateThumbprint = NormalizeThumbprint(certificateThumbprint);
        Description = description;
        PairingId = pairingId;
        AccessToken = accessToken;
    }

    public Uri BackendAddress { get; }

    public string CertificateThumbprint { get; }

    public string Description { get; }

    public Guid PairingId { get; }

    public string AccessToken { get; }

    public static string NormalizeThumbprint(string thumbprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);

        return thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }
}
