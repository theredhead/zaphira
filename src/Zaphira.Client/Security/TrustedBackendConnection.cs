namespace Zaphira.Client.Security;

public sealed record TrustedBackendConnection
{
    public TrustedBackendConnection(Uri backendAddress, string certificateThumbprint, string description)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateThumbprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (!backendAddress.IsAbsoluteUri)
        {
            throw new ArgumentException("Backend address must be absolute.", nameof(backendAddress));
        }

        BackendAddress = backendAddress;
        CertificateThumbprint = NormalizeThumbprint(certificateThumbprint);
        Description = description;
    }

    public Uri BackendAddress { get; }

    public string CertificateThumbprint { get; }

    public string Description { get; }

    public static string NormalizeThumbprint(string thumbprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);

        return thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }
}
