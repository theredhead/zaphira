namespace Zaphira.Contracts;

public sealed record CreatePairingResponse
{
    public CreatePairingResponse(
        Guid pairingId,
        string accessToken,
        string backendCertificateThumbprint,
        string backendDescription)
    {
        if (pairingId == Guid.Empty)
        {
            throw new ArgumentException("Pairing identifier cannot be empty.", nameof(pairingId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(backendCertificateThumbprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(backendDescription);

        PairingId = pairingId;
        AccessToken = accessToken;
        BackendCertificateThumbprint = backendCertificateThumbprint;
        BackendDescription = backendDescription;
    }

    public Guid PairingId { get; }

    public string AccessToken { get; }

    public string BackendCertificateThumbprint { get; }

    public string BackendDescription { get; }
}
